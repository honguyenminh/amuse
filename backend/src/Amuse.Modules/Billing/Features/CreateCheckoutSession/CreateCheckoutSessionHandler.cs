using System.Security.Claims;
using Amuse.Domain.Billing;
using Amuse.Domain.Catalog;
using Amuse.Domain.Identity;
using Amuse.Domain.SharedKernel;
using Amuse.Domain.Tenancy;
using Amuse.Modules.Billing.Contracts;
using Amuse.Modules.Billing.Features.Common;
using Amuse.Modules.Billing.Persistence;
using Amuse.Modules.Catalog.Contracts;
using Amuse.Modules.Common.Time;
using Amuse.Modules.Discovery.Features.Common;
using Amuse.Modules.Identity.Contracts;
using Amuse.Modules.Identity.Persistence;
using Amuse.Modules.Tenancy.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Amuse.Modules.Billing.Features.CreateCheckoutSession;

internal sealed class CreateCheckoutSessionHandler(
    BillingDbContext billingDb,
    IdentityDbContext identityDb,
    ICatalogPurchaseReadModel catalog,
    ITenancyOrganizationReadModel tenancy,
    IListenerPersonaReadModel personaReadModel,
    ICheckoutProvider checkoutProvider,
    IOptions<CheckoutConfig> checkoutOptions,
    IClock clock)
{
    public async Task<Result<CheckoutSessionResponse>> HandleAsync(
        CreateCheckoutSessionRequest request,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var listenerResult = await DiscoveryPrincipal.RequireListenerAsync(
            principal, personaReadModel, cancellationToken);
        if (!listenerResult.IsSuccess)
            return Result<CheckoutSessionResponse>.Failure(listenerResult.Error!);

        var accountId = listenerResult.Value!.AccountId;

        var banned = await identityDb.Accounts.AsNoTracking()
            .AnyAsync(a => a.Id == accountId && a.Status == AccountStatus.Banned, cancellationToken);
        if (banned)
            return Result<CheckoutSessionResponse>.Failure(BillingErrors.AccountBanned);

        if (request.TrackId is { } trackId)
            return await CreateTrackCheckoutAsync(accountId, trackId, request.AmountMinor, cancellationToken);

        if (request.ReleaseId is { } releaseId)
            return await CreateReleaseCheckoutAsync(accountId, releaseId, request.AmountMinor, cancellationToken);

        return Result<CheckoutSessionResponse>.Failure(BillingErrors.InvalidAcquisitionTarget);
    }

    private async Task<Result<CheckoutSessionResponse>> CreateTrackCheckoutAsync(
        AccountId accountId,
        Guid trackId,
        long amountMinor,
        CancellationToken cancellationToken)
    {
        var checkoutTrack = await catalog.GetCheckoutTrackAsync(TrackId.From(trackId), cancellationToken);
        if (checkoutTrack is null)
            return Result<CheckoutSessionResponse>.Failure(BillingErrors.TrackNotFound);

        var lifecycle = await tenancy.GetLifecycleStatusAsync(checkoutTrack.OrganizationId, cancellationToken);
        if (lifecycle is OrganizationLifecycleStatus.Suspended or OrganizationLifecycleStatus.Closed)
            return Result<CheckoutSessionResponse>.Failure(BillingErrors.OrgSalesBlocked);

        var pricing = CheckoutPricingGuard.ValidateAmount(
            amountMinor,
            checkoutTrack.PriceFloorMinor,
            checkoutTrack.PriceCeilingMinor);
        if (!pricing.IsSuccess)
            return Result<CheckoutSessionResponse>.Failure(pricing.Error!);

        var duplicate = await billingDb.Purchases.AnyAsync(
            p => p.AccountId == accountId
                 && p.TrackId == trackId
                 && p.EntitlementStatus == EntitlementStatus.Active,
            cancellationToken);
        if (duplicate)
            return Result<CheckoutSessionResponse>.Failure(BillingErrors.PurchaseDuplicate);

        var pendingDuplicate = await billingDb.Purchases.AnyAsync(
            p => p.AccountId == accountId
                 && p.TrackId == trackId
                 && p.PaymentStatus == PaymentStatus.Pending,
            cancellationToken);
        if (pendingDuplicate)
            return Result<CheckoutSessionResponse>.Failure(BillingErrors.PurchaseDuplicate);

        var money = Money.Create(amountMinor, checkoutTrack.Currency);
        if (!money.IsSuccess)
            return Result<CheckoutSessionResponse>.Failure(money.Error!);

        return await CreateSessionCoreAsync(
            accountId,
            checkoutTrack.OrganizationId,
            PurchasedUnit.Track,
            trackId,
            null,
            checkoutTrack.Title,
            money.Value!,
            cancellationToken);
    }

    private async Task<Result<CheckoutSessionResponse>> CreateReleaseCheckoutAsync(
        AccountId accountId,
        Guid releaseId,
        long amountMinor,
        CancellationToken cancellationToken)
    {
        var checkoutRelease = await catalog.GetCheckoutReleaseAsync(ReleaseId.From(releaseId), cancellationToken);
        if (checkoutRelease is null)
            return Result<CheckoutSessionResponse>.Failure(BillingErrors.ReleaseNotFound);

        var lifecycle = await tenancy.GetLifecycleStatusAsync(checkoutRelease.OrganizationId, cancellationToken);
        if (lifecycle is OrganizationLifecycleStatus.Suspended or OrganizationLifecycleStatus.Closed)
            return Result<CheckoutSessionResponse>.Failure(BillingErrors.OrgSalesBlocked);

        var pricing = CheckoutPricingGuard.ValidateAmount(
            amountMinor,
            checkoutRelease.PriceFloorMinor,
            checkoutRelease.PriceCeilingMinor);
        if (!pricing.IsSuccess)
            return Result<CheckoutSessionResponse>.Failure(pricing.Error!);

        var duplicate = await billingDb.Purchases.AnyAsync(
            p => p.AccountId == accountId
                 && p.ReleaseId == releaseId
                 && p.PurchasedUnit == PurchasedUnit.Release
                 && p.EntitlementStatus == EntitlementStatus.Active,
            cancellationToken);
        if (duplicate)
            return Result<CheckoutSessionResponse>.Failure(BillingErrors.PurchaseReleaseDuplicate);

        var pendingDuplicate = await billingDb.Purchases.AnyAsync(
            p => p.AccountId == accountId
                 && p.ReleaseId == releaseId
                 && p.PurchasedUnit == PurchasedUnit.Release
                 && p.PaymentStatus == PaymentStatus.Pending,
            cancellationToken);
        if (pendingDuplicate)
            return Result<CheckoutSessionResponse>.Failure(BillingErrors.PurchaseReleaseDuplicate);

        var money = Money.Create(amountMinor, checkoutRelease.Currency);
        if (!money.IsSuccess)
            return Result<CheckoutSessionResponse>.Failure(money.Error!);

        return await CreateSessionCoreAsync(
            accountId,
            checkoutRelease.OrganizationId,
            PurchasedUnit.Release,
            null,
            releaseId,
            checkoutRelease.Title,
            money.Value!,
            cancellationToken);
    }

    private async Task<Result<CheckoutSessionResponse>> CreateSessionCoreAsync(
        AccountId accountId,
        OrganizationId organizationId,
        PurchasedUnit unit,
        Guid? trackId,
        Guid? releaseId,
        string productName,
        Money amount,
        CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var purchase = unit == PurchasedUnit.Track
            ? Purchase.CreatePaidTrack(accountId, organizationId, trackId!.Value, amount, now)
            : Purchase.CreatePaidRelease(accountId, organizationId, releaseId!.Value, amount, now);

        if (!purchase.IsSuccess)
            return Result<CheckoutSessionResponse>.Failure(purchase.Error!);

        var paymentTransaction = PaymentTransaction.CreatePending(
            purchase.Value!.Id,
            accountId,
            amount,
            now);

        if (!paymentTransaction.IsSuccess)
            return Result<CheckoutSessionResponse>.Failure(paymentTransaction.Error!);

        var config = checkoutOptions.Value;
        if (string.IsNullOrWhiteSpace(config.SuccessUrl) || string.IsNullOrWhiteSpace(config.CancelUrl))
            return Result<CheckoutSessionResponse>.Failure(BillingErrors.CheckoutNotConfigured);

        var sessionResult = await checkoutProvider.CreateSessionAsync(
            new CheckoutSessionRequest(
                purchase.Value!.Id.Value,
                paymentTransaction.Value!.Id.Value,
                amount.AmountMinor,
                amount.Currency,
                productName,
                config.SuccessUrl,
                config.CancelUrl,
                accountId.Value),
            cancellationToken);

        if (!sessionResult.IsSuccess)
            return Result<CheckoutSessionResponse>.Failure(sessionResult.Error!);

        var assignSession = paymentTransaction.Value!.AssignCheckoutSession(sessionResult.Value!.SessionId);
        if (!assignSession.IsSuccess)
            return Result<CheckoutSessionResponse>.Failure(assignSession.Error!);

        billingDb.Purchases.Add(purchase.Value!);
        billingDb.PaymentTransactions.Add(paymentTransaction.Value!);
        await billingDb.SaveChangesAsync(cancellationToken);

        return Result<CheckoutSessionResponse>.Success(new CheckoutSessionResponse(
            purchase.Value!.Id.Value,
            sessionResult.Value!.SessionId,
            sessionResult.Value!.CheckoutUrl));
    }
}
