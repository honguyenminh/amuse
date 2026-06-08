using Microsoft.Extensions.Logging;

namespace Amuse.Modules.Billing.Services;

internal sealed partial class StripeGlobalPayoutProvider
{
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Stripe recipient creation failed for org {OrganizationId}")]
    private partial void LogRecipientCreationFailed(Exception ex, Guid organizationId);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Stripe account link creation failed for recipient {RecipientId}")]
    private partial void LogAccountLinkCreationFailed(Exception ex, string recipientId);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Stripe outbound transfer failed for withdrawal {WithdrawalId}")]
    private partial void LogOutboundTransferFailed(Exception ex, Guid withdrawalId);
}
