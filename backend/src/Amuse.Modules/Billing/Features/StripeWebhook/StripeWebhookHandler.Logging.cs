using Microsoft.Extensions.Logging;

namespace Amuse.Modules.Billing.Features.StripeWebhook;

internal sealed partial class StripeWebhookHandler
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Stripe webhook signature validation failed")]
    private partial void LogSignatureValidationFailed(Exception ex);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Stripe charge dispute closed: {DisputeId} status={Status} charge={ChargeId}")]
    private partial void LogDisputeClosed(string disputeId, string status, string chargeId);
}
