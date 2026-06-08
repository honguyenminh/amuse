using System.Security.Claims;
using Amuse.Domain.Billing;
using Amuse.Domain.Identity;
using Amuse.Domain.Platform;
using Amuse.Domain.SharedKernel;
using Amuse.Domain.Tenancy;
using Amuse.Modules.Billing.Contracts;
using Amuse.Modules.Billing.Features.PayoutProfile;
using Amuse.Modules.Billing.Persistence;
using Amuse.Modules.Billing.Services;
using Amuse.Modules.Common.Time;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Billing.Features.RefundPurchase;

internal sealed class RefundPurchaseHandler(
    BillingDbContext billingDb,
    ICheckoutProvider checkoutProvider,
    RefundCompletionService refundCompletionService,
    IClock clock)
{
    public async Task<Result<RefundPurchaseResponse>> HandleAsync(
        Guid purchaseIdValue,
        RefundPurchaseRequest request,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var auth = ResolveAuthorization(principal);
        if (!auth.IsSuccess)
            return Result<RefundPurchaseResponse>.Failure(auth.Error!);

        var purchaseId = PurchaseId.From(purchaseIdValue);
        var purchase = await billingDb.Purchases
            .FirstOrDefaultAsync(p => p.Id == purchaseId, cancellationToken);

        if (purchase is null)
            return Result<RefundPurchaseResponse>.Failure(BillingErrors.PurchaseNotFound);

        if (purchase.PaymentStatus == PaymentStatus.Refunded)
            return Result<RefundPurchaseResponse>.Failure(BillingErrors.RefundAlreadyProcessed);

        if (purchase.PaymentStatus != PaymentStatus.Paid)
            return Result<RefundPurchaseResponse>.Failure(BillingErrors.RefundNotEligible);

        var feeBearerResult = ResolveFeeBearer(auth.Value!, request.RefundFeeBearer);
        if (!feeBearerResult.IsSuccess)
            return Result<RefundPurchaseResponse>.Failure(feeBearerResult.Error!);

        if (auth.Value!.InitiatorRole == RefundInitiatorRole.Seller)
        {
            var orgId = auth.Value!.OrganizationId!.Value;
            var isPayee = await billingDb.PurchaseAllocationSnapshots.AsNoTracking()
                .AnyAsync(
                    snapshot => snapshot.PurchaseId == purchase.Id
                                && snapshot.PayeeOrganizationId == orgId,
                    cancellationToken);

            if (!isPayee)
                return Result<RefundPurchaseResponse>.Failure(BillingErrors.RefundNotAllowed);
        }

        var paymentTransaction = await billingDb.PaymentTransactions
            .FirstOrDefaultAsync(
                transaction => transaction.PurchaseId == purchase.Id
                               && transaction.Status == PaymentStatus.Paid,
                cancellationToken);

        if (paymentTransaction is null || string.IsNullOrWhiteSpace(paymentTransaction.ProviderReference))
            return Result<RefundPurchaseResponse>.Failure(BillingErrors.PurchaseNotFound);

        var now = clock.UtcNow;
        var beginRefund = purchase.BeginRefund(
            auth.Value!.InitiatedBy,
            auth.Value!.InitiatorRole,
            request.Reason,
            feeBearerResult.Value!,
            now);

        if (!beginRefund.IsSuccess)
            return Result<RefundPurchaseResponse>.Failure(beginRefund.Error!);

        var providerRefund = await checkoutProvider.RefundChargeAsync(
            paymentTransaction.ProviderReference,
            cancellationToken);

        if (!providerRefund.IsSuccess)
            return Result<RefundPurchaseResponse>.Failure(providerRefund.Error!);

        var complete = await refundCompletionService.CompleteAsync(
            purchase,
            paymentTransaction,
            providerRefund.Value!.RefundFeeMinor,
            cancellationToken);

        if (!complete.IsSuccess)
            return Result<RefundPurchaseResponse>.Failure(complete.Error!);

        return Result<RefundPurchaseResponse>.Success(new RefundPurchaseResponse(
            purchase.Id.Value,
            purchase.PaymentStatus.ToString().ToLowerInvariant(),
            purchase.RefundedAt!.Value));
    }

    private static Result<RefundAuthorizationContext> ResolveAuthorization(ClaimsPrincipal principal)
    {
        var ctx = principal.FindFirst("ctx")?.Value;
        var accountIdValue = principal.FindFirst("sub")?.Value
            ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(accountIdValue, out var accountGuid))
            return Result<RefundAuthorizationContext>.Failure(IdentityErrors.InvalidPersonaContext);

        var initiatedBy = AccountId.From(accountGuid);
        var claims = principal.FindAll("claims").Select(claim => claim.Value).ToList();

        if (string.Equals(ctx, "platform", StringComparison.OrdinalIgnoreCase))
        {
            if (!PlatformClaims.CanManagePurchases(claims))
                return Result<RefundAuthorizationContext>.Failure(BillingErrors.RefundNotAllowed);

            return Result<RefundAuthorizationContext>.Success(new RefundAuthorizationContext(
                initiatedBy,
                RefundInitiatorRole.Platform,
                null));
        }

        if (string.Equals(ctx, "org", StringComparison.OrdinalIgnoreCase))
        {
            var orgResult = BillingPersonaAccessor.GetOrganizationId(principal);
            if (!orgResult.IsSuccess)
                return Result<RefundAuthorizationContext>.Failure(orgResult.Error!);

            if (!OrgClaim.MatchesAny(
                    [OrgClaim.ScopeSubClaim("manage", "purchase", "refund")],
                    claims.ToHashSet(StringComparer.Ordinal)))
            {
                return Result<RefundAuthorizationContext>.Failure(BillingErrors.RefundNotAllowed);
            }

            return Result<RefundAuthorizationContext>.Success(new RefundAuthorizationContext(
                initiatedBy,
                RefundInitiatorRole.Seller,
                orgResult.Value));
        }

        return Result<RefundAuthorizationContext>.Failure(IdentityErrors.InvalidPersonaContext);
    }

    private static Result<RefundFeeBearer> ResolveFeeBearer(
        RefundAuthorizationContext auth,
        string? refundFeeBearer)
    {
        if (auth.InitiatorRole == RefundInitiatorRole.Seller)
            return Result<RefundFeeBearer>.Success(RefundFeeBearer.Seller);

        if (string.IsNullOrWhiteSpace(refundFeeBearer))
            return Result<RefundFeeBearer>.Failure(BillingErrors.RefundFeeBearerRequired);

        return refundFeeBearer.Trim().ToLowerInvariant() switch
        {
            "platform" => Result<RefundFeeBearer>.Success(RefundFeeBearer.Platform),
            "seller" => Result<RefundFeeBearer>.Success(RefundFeeBearer.Seller),
            _ => Result<RefundFeeBearer>.Failure(BillingErrors.RefundFeeBearerRequired),
        };
    }

    private sealed record RefundAuthorizationContext(
        AccountId InitiatedBy,
        RefundInitiatorRole InitiatorRole,
        OrganizationId? OrganizationId);
}
