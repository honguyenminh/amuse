using System.Security.Claims;
using Amuse.Domain.Billing;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Billing.Contracts;
using Amuse.Modules.Billing.Persistence;
using Amuse.Modules.Common.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Amuse.Modules.Billing.Features.PayoutProfile;

public sealed record StripeAccountLinkResponse(string Url, DateTimeOffset ExpiresAt);

internal sealed class CreateStripeAccountLinkHandler(
    BillingDbContext billingDb,
    IGlobalPayoutProvider payoutProvider,
    IOptions<GlobalPayoutConfig> payoutConfig,
    IClock clock)
{
    public async Task<Result<StripeAccountLinkResponse>> HandleAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var orgResult = BillingPersonaAccessor.GetOrganizationId(principal);
        if (!orgResult.IsSuccess)
            return Result<StripeAccountLinkResponse>.Failure(orgResult.Error!);

        var organizationId = orgResult.Value!;
        var now = clock.UtcNow;

        var profile = await billingDb.PayoutProfiles
            .SingleOrDefaultAsync(p => p.OrganizationId == organizationId, cancellationToken);

        if (profile is null)
            return Result<StripeAccountLinkResponse>.Failure(BillingErrors.PayoutProfileNotFound);

        if (profile.PayoutRail != PayoutRail.StripeGlobal)
            return Result<StripeAccountLinkResponse>.Failure(BillingErrors.PayoutProfileInvalidStatusTransition);

        if (string.IsNullOrWhiteSpace(profile.CountryCode) || string.IsNullOrWhiteSpace(profile.LegalName))
            return Result<StripeAccountLinkResponse>.Failure(BillingErrors.PayoutProfileIncomplete);

        var config = payoutConfig.Value;
        if (string.IsNullOrWhiteSpace(config.AccountLinkReturnUrl)
            || string.IsNullOrWhiteSpace(config.AccountLinkRefreshUrl))
        {
            return Result<StripeAccountLinkResponse>.Failure(BillingErrors.PayoutNotConfigured);
        }

        if (string.IsNullOrWhiteSpace(profile.ExternalRecipientId))
        {
            var recipientResult = await payoutProvider.EnsureRecipientAsync(
                new EnsureRecipientRequest(
                    organizationId.Value,
                    profile.CountryCode,
                    $"{organizationId.Value:N}@payout.amuse.local",
                    profile.LegalName),
                cancellationToken);

            if (!recipientResult.IsSuccess)
                return Result<StripeAccountLinkResponse>.Failure(recipientResult.Error!);

            var setRecipient = profile.SetExternalRecipientId(recipientResult.Value!.RecipientId, now);
            if (!setRecipient.IsSuccess)
                return Result<StripeAccountLinkResponse>.Failure(setRecipient.Error!);

            await billingDb.SaveChangesAsync(cancellationToken);
        }

        var linkResult = await payoutProvider.CreateAccountLinkAsync(
            new AccountLinkRequest(
                profile.ExternalRecipientId!,
                config.AccountLinkReturnUrl,
                config.AccountLinkRefreshUrl),
            cancellationToken);

        if (!linkResult.IsSuccess)
            return Result<StripeAccountLinkResponse>.Failure(linkResult.Error!);

        return Result<StripeAccountLinkResponse>.Success(
            new StripeAccountLinkResponse(linkResult.Value!.Url, linkResult.Value.ExpiresAt));
    }
}
