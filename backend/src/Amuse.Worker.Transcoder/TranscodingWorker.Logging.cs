using Microsoft.Extensions.Logging;

namespace Amuse.Worker.Transcoder;

internal sealed partial class TranscodingWorker
{
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Transcoder worker starting; RabbitMQ {RabbitHost}:{RabbitPort}, queue {QueueName}, prefetch {PrefetchCount}")]
    private partial void LogWorkerStarting(string rabbitHost, int rabbitPort, string queueName, ushort prefetchCount);

    [LoggerMessage(Level = LogLevel.Information, Message = "Transcoder worker consuming queue {QueueName}")]
    private partial void LogWorkerConsuming(string queueName);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Ignoring RabbitMQ message with invalid payload on delivery {DeliveryTag}")]
    private partial void LogInvalidPayload(ulong deliveryTag);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Received transcode message job {JobId} track {TrackId} delivery {DeliveryTag}")]
    private partial void LogMessageReceived(Guid jobId, Guid trackId, ulong deliveryTag);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Transcode job {JobId} not found in database; acking delivery {DeliveryTag}")]
    private partial void LogJobNotFound(Guid jobId, ulong deliveryTag);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Transcode job {JobId} for track {TrackId} already succeeded; acking delivery {DeliveryTag}")]
    private partial void LogJobAlreadySucceeded(Guid jobId, Guid trackId, ulong deliveryTag);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Transcode job {JobId} for track {TrackId} marked processing (attempt {AttemptCount}); master {MasterKey} -> stream {StreamKey}")]
    private partial void LogJobProcessing(
        Guid jobId,
        Guid trackId,
        int attemptCount,
        string masterKey,
        string streamKey);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "DASH manifest already present at {StreamKey} for job {JobId}; skipping ffmpeg")]
    private partial void LogDashManifestPresent(string streamKey, Guid jobId);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Transcode job {JobId} for track {TrackId} succeeded in {ElapsedMs}ms; stream {StreamKey}")]
    private partial void LogJobSucceeded(Guid jobId, Guid trackId, long elapsedMs, string streamKey);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Transcode job {JobId} for track {TrackId} failed after {ElapsedMs}ms (attempt {AttemptCount})")]
    private partial void LogJobFailed(Exception ex, Guid? jobId, Guid? trackId, long elapsedMs, int? attemptCount);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Transcode job {JobId} for track {TrackId} persisted failure: {LastError}")]
    private partial void LogJobFailurePersisted(Guid jobId, Guid trackId, string lastError);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to persist transcode job {JobId} failure state")]
    private partial void LogJobFailurePersistError(Exception ex, Guid jobId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Transcoder worker shutdown requested")]
    private partial void LogWorkerShutdownRequested();

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Starting DASH packaging for job {JobId} track {TrackId}; master {MasterKey} -> {StreamKey}")]
    private partial void LogDashPackagingStarting(
        Guid jobId,
        Guid trackId,
        string masterKey,
        string streamKey);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "DASH work directory for job {JobId} track {TrackId}: {WorkDirectory}")]
    private partial void LogDashWorkDirectory(Guid jobId, Guid trackId, string workDirectory);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Uploading {ArtifactCount} DASH artifacts for job {JobId} track {TrackId} under prefix {StoragePrefix}")]
    private partial void LogDashUploading(
        int artifactCount,
        Guid jobId,
        Guid trackId,
        string storagePrefix);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Uploaded DASH artifact {ObjectKey} ({ByteCount} bytes) for job {JobId} track {TrackId}")]
    private partial void LogDashArtifactUploaded(string objectKey, int byteCount, Guid jobId, Guid trackId);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "DASH packaging completed for job {JobId} track {TrackId} in {ElapsedMs}ms")]
    private partial void LogDashPackagingCompleted(Guid jobId, Guid trackId, long elapsedMs);

    [LoggerMessage(Level = LogLevel.Information, Message = "Starting ffmpeg for job {JobId} track {TrackId}")]
    private partial void LogFfmpegStarting(Guid jobId, Guid trackId);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "ffmpeg failed for job {JobId} track {TrackId} with exit code {ExitCode} after {ElapsedMs}ms; stderr: {FfmpegStderr}")]
    private partial void LogFfmpegFailed(
        Guid jobId,
        Guid trackId,
        int exitCode,
        long elapsedMs,
        string ffmpegStderr);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "ffmpeg stdout for job {JobId} track {TrackId}: {FfmpegStdout}")]
    private partial void LogFfmpegStdout(Guid jobId, Guid trackId, string ffmpegStdout);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "ffmpeg completed for job {JobId} track {TrackId} in {ElapsedMs}ms")]
    private partial void LogFfmpegCompleted(Guid jobId, Guid trackId, long elapsedMs);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "ffmpeg stderr for job {JobId} track {TrackId}: {FfmpegStderr}")]
    private partial void LogFfmpegStderr(Guid jobId, Guid trackId, string ffmpegStderr);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "RabbitMQ unreachable at {RabbitHost}:{RabbitPort}; retrying in {RetrySeconds}s")]
    private partial void LogRabbitMqUnreachable(
        Exception ex,
        string rabbitHost,
        int rabbitPort,
        double retrySeconds);
}
