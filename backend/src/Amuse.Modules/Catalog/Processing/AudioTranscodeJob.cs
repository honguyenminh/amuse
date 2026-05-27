using Amuse.Domain.Catalog;

namespace Amuse.Modules.Catalog.Processing;

public enum AudioTranscodeJobStatus
{
    Queued = 1,
    Processing = 2,
    Succeeded = 3,
    Failed = 4,
}

public sealed class AudioTranscodeJob
{
    public Guid Id { get; private set; }
    public TrackId TrackId { get; private set; }
    public string MasterKey { get; private set; } = null!;
    public string StreamKey { get; private set; } = null!;
    public AudioTranscodeJobStatus Status { get; private set; }
    public int AttemptCount { get; private set; }
    public string? LastError { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private AudioTranscodeJob()
    {
    }

    private AudioTranscodeJob(
        Guid id,
        TrackId trackId,
        string masterKey,
        string streamKey,
        DateTimeOffset now)
    {
        Id = id;
        TrackId = trackId;
        MasterKey = masterKey;
        StreamKey = streamKey;
        Status = AudioTranscodeJobStatus.Queued;
        AttemptCount = 0;
        CreatedAt = now;
        UpdatedAt = now;
    }

    public static AudioTranscodeJob Enqueue(
        TrackId trackId,
        string masterKey,
        string streamKey,
        DateTimeOffset now) =>
        new(Guid.CreateVersion7(), trackId, masterKey, streamKey, now);

    public void MarkProcessing(DateTimeOffset now)
    {
        Status = AudioTranscodeJobStatus.Processing;
        AttemptCount++;
        UpdatedAt = now;
        LastError = null;
    }

    public void MarkSucceeded(DateTimeOffset now)
    {
        Status = AudioTranscodeJobStatus.Succeeded;
        UpdatedAt = now;
        LastError = null;
    }

    public void MarkFailed(string error, DateTimeOffset now)
    {
        Status = AudioTranscodeJobStatus.Failed;
        UpdatedAt = now;
        LastError = string.IsNullOrWhiteSpace(error) ? "Unknown error" : error;
    }
}

