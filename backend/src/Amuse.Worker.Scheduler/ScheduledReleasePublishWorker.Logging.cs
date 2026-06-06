using Microsoft.Extensions.Logging;

internal sealed partial class ScheduledReleasePublishWorker
{
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Catalog scheduler starting; poll {PollIntervalSeconds}s, batch {BatchSize}, claim FOR UPDATE SKIP LOCKED")]
    private partial void LogSchedulerStarting(double pollIntervalSeconds, int batchSize);

    [LoggerMessage(Level = LogLevel.Error, Message = "Catalog scheduler iteration failed.")]
    private partial void LogSchedulerIterationFailed(Exception ex);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Scheduled publish skipped for release {ReleaseId} due to {ErrorCode}")]
    private partial void LogScheduledPublishSkipped(Guid releaseId, string errorCode);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Catalog scheduler processed due releases; published {PublishedCount}, skipped {SkippedCount}")]
    private partial void LogSchedulerProcessedDueReleases(int publishedCount, int skippedCount);
}
