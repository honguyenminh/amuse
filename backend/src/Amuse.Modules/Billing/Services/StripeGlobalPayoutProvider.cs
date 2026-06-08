using Amuse.Domain.Billing;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Billing.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;

namespace Amuse.Modules.Billing.Services;

internal sealed class StripeGlobalPayoutProvider(
    IOptions<StripeConfig> stripeOptions,
    ILogger<StripeGlobalPayoutProvider> logger) : IGlobalPayoutProvider
{
    public async Task<Result<StripeRecipientResult>> EnsureRecipientAsync(
        EnsureRecipientRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryConfigureApiKey(out var failure))
            return Result<StripeRecipientResult>.Failure(failure);

        try
        {
            var service = new AccountService();
            var account = await service.CreateAsync(
                new AccountCreateOptions
                {
                    Type = "custom",
                    Country = request.CountryCode,
                    Email = request.Email,
                    Capabilities = new AccountCapabilitiesOptions
                    {
                        Transfers = new AccountCapabilitiesTransfersOptions { Requested = true },
                    },
                    BusinessType = "individual",
                    Metadata = new Dictionary<string, string>
                    {
                        ["organization_id"] = request.OrganizationId.ToString(),
                        ["legal_name"] = request.LegalName,
                    },
                },
                cancellationToken: cancellationToken);

            if (string.IsNullOrWhiteSpace(account.Id))
                return Result<StripeRecipientResult>.Failure(BillingErrors.PayoutNotConfigured);

            return Result<StripeRecipientResult>.Success(new StripeRecipientResult(account.Id));
        }
        catch (StripeException ex)
        {
            logger.LogError(ex, "Stripe recipient creation failed for org {OrganizationId}", request.OrganizationId);
            return Result<StripeRecipientResult>.Failure(BillingErrors.PayoutNotConfigured);
        }
    }

    public async Task<Result<AccountLinkResult>> CreateAccountLinkAsync(
        AccountLinkRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryConfigureApiKey(out var failure))
            return Result<AccountLinkResult>.Failure(failure);

        try
        {
            var service = new AccountLinkService();
            var link = await service.CreateAsync(
                new AccountLinkCreateOptions
                {
                    Account = request.RecipientId,
                    RefreshUrl = request.RefreshUrl,
                    ReturnUrl = request.ReturnUrl,
                    Type = "account_onboarding",
                },
                cancellationToken: cancellationToken);

            if (string.IsNullOrWhiteSpace(link.Url))
                return Result<AccountLinkResult>.Failure(BillingErrors.PayoutNotConfigured);

            var expiresAt = link.ExpiresAt <= DateTime.UnixEpoch
                ? DateTimeOffset.UtcNow.AddHours(1)
                : new DateTimeOffset(link.ExpiresAt, TimeSpan.Zero);

            return Result<AccountLinkResult>.Success(new AccountLinkResult(link.Url, expiresAt));
        }
        catch (StripeException ex)
        {
            logger.LogError(ex, "Stripe account link creation failed for recipient {RecipientId}", request.RecipientId);
            return Result<AccountLinkResult>.Failure(BillingErrors.PayoutNotConfigured);
        }
    }

    public async Task<Result<OutboundPaymentResult>> SubmitOutboundPaymentAsync(
        OutboundPaymentRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryConfigureApiKey(out var failure))
            return Result<OutboundPaymentResult>.Failure(failure);

        try
        {
            var service = new TransferService();
            var transfer = await service.CreateAsync(
                new TransferCreateOptions
                {
                    Amount = request.AmountMinor,
                    Currency = request.Currency.ToLowerInvariant(),
                    Destination = request.RecipientId,
                    Metadata = new Dictionary<string, string>
                    {
                        ["withdrawal_id"] = request.WithdrawalId.ToString(),
                    },
                },
                cancellationToken: cancellationToken);

            if (string.IsNullOrWhiteSpace(transfer.Id))
                return Result<OutboundPaymentResult>.Failure(BillingErrors.PayoutOutboundFailed);

            return Result<OutboundPaymentResult>.Success(new OutboundPaymentResult(transfer.Id));
        }
        catch (StripeException ex)
        {
            logger.LogError(
                ex,
                "Stripe outbound transfer failed for withdrawal {WithdrawalId}",
                request.WithdrawalId);
            return Result<OutboundPaymentResult>.Failure(BillingErrors.PayoutOutboundFailed);
        }
    }

    private bool TryConfigureApiKey(out DomainError failure)
    {
        failure = BillingErrors.PayoutNotConfigured;
        var secretKey = stripeOptions.Value.SecretKey;
        if (string.IsNullOrWhiteSpace(secretKey))
            return false;

        StripeConfiguration.ApiKey = secretKey;
        return true;
    }
}
