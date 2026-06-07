using System.Text.Json;
using Amuse.Modules.Catalog.Processing;

namespace Amuse.Modules.Catalog.Messaging;

public sealed class CatalogOutboxMessage
{
    public const string AudioTranscodeJobType = "catalog.audio_transcode_job";

    public Guid Id { get; private set; }
    public string MessageType { get; private set; } = null!;
    public string PayloadJson { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ProcessedAt { get; private set; }
    public string? LastError { get; private set; }
    public int AttemptCount { get; private set; }

    private CatalogOutboxMessage()
    {
    }

    private CatalogOutboxMessage(
        Guid id,
        string messageType,
        string payloadJson,
        DateTimeOffset createdAt)
    {
        Id = id;
        MessageType = messageType;
        PayloadJson = payloadJson;
        CreatedAt = createdAt;
        AttemptCount = 0;
    }

    public static CatalogOutboxMessage EnqueueAudioTranscode(
        AudioTranscodeJobMessage message,
        DateTimeOffset now) =>
        new(
            Guid.CreateVersion7(),
            AudioTranscodeJobType,
            JsonSerializer.Serialize(message),
            now);

    public void MarkProcessed(DateTimeOffset now)
    {
        ProcessedAt = now;
        LastError = null;
    }

    public void MarkAttemptFailed(string error, DateTimeOffset now)
    {
        AttemptCount++;
        LastError = string.IsNullOrWhiteSpace(error) ? "Unknown error" : error;
        CreatedAt = now;
    }
}
