using Amuse.Domain.SharedKernel;

namespace Amuse.Modules.Billing.Contracts;

public sealed record EnsureRecipientRequest(
    Guid OrganizationId,
    string CountryCode,
    string Email,
    string LegalName);

public sealed record StripeRecipientResult(string RecipientId);

public sealed record AccountLinkRequest(
    string RecipientId,
    string ReturnUrl,
    string RefreshUrl);

public sealed record AccountLinkResult(string Url, DateTimeOffset ExpiresAt);

public sealed record OutboundPaymentRequest(
    Guid WithdrawalId,
    string RecipientId,
    long AmountMinor,
    string Currency);

public sealed record OutboundPaymentResult(string TransferId);

public interface IGlobalPayoutProvider
{
    Task<Result<StripeRecipientResult>> EnsureRecipientAsync(
        EnsureRecipientRequest request,
        CancellationToken cancellationToken);

    Task<Result<AccountLinkResult>> CreateAccountLinkAsync(
        AccountLinkRequest request,
        CancellationToken cancellationToken);

    Task<Result<OutboundPaymentResult>> SubmitOutboundPaymentAsync(
        OutboundPaymentRequest request,
        CancellationToken cancellationToken);
}
