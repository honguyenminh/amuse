using Microsoft.Extensions.Logging;

namespace Amuse.Modules.Billing.Services;

internal sealed partial class StripeWithdrawalExecutionService
{
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Stripe outbound payout failed for withdrawal {WithdrawalId}: {ErrorCode}")]
    private partial void LogStripeOutboundPayoutFailed(Guid withdrawalId, string? errorCode);
}
