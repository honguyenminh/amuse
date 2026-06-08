using Microsoft.Extensions.Logging;

namespace Amuse.Modules.Billing.Services;

internal sealed partial class PendingToAvailableWorker
{
    [LoggerMessage(Level = LogLevel.Error, Message = "Pending-to-available worker iteration failed")]
    private partial void LogIterationFailed(Exception ex);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Skipping hold release for purchase {PurchaseId}: {ErrorCode}")]
    private partial void LogHoldReleaseSkipped(Guid purchaseId, string? errorCode);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Released pending seller credits for {Count} purchases")]
    private partial void LogReleasedPendingCredits(int count);
}
