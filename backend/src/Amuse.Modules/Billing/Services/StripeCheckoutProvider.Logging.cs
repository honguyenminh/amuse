using Microsoft.Extensions.Logging;

namespace Amuse.Modules.Billing.Services;

internal sealed partial class StripeCheckoutProvider
{
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Stripe checkout session creation failed for purchase {PurchaseId}")]
    private partial void LogSessionCreationFailed(Exception ex, Guid purchaseId);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Stripe checkout session lookup failed for session {SessionId}")]
    private partial void LogSessionLookupFailed(Exception ex, string sessionId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Stripe refund failed for charge {ChargeId}")]
    private partial void LogRefundFailed(Exception ex, string chargeId);
}
