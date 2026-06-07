using Amuse.Domain.Catalog;

namespace Amuse.Modules.Catalog.Processing;

public sealed class AudioMasterUploadIntent
{
    public Guid Id { get; private set; }
    public TrackId TrackId { get; private set; }
    public string ObjectKey { get; private set; } = null!;
    public string ContentType { get; private set; } = null!;
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ConsumedAt { get; private set; }

    private AudioMasterUploadIntent()
    {
    }

    private AudioMasterUploadIntent(
        Guid id,
        TrackId trackId,
        string objectKey,
        string contentType,
        DateTimeOffset expiresAt,
        DateTimeOffset createdAt)
    {
        Id = id;
        TrackId = trackId;
        ObjectKey = objectKey;
        ContentType = contentType;
        ExpiresAt = expiresAt;
        CreatedAt = createdAt;
    }

    public static AudioMasterUploadIntent Create(
        TrackId trackId,
        string objectKey,
        string contentType,
        DateTimeOffset expiresAt,
        DateTimeOffset createdAt) =>
        new(Guid.CreateVersion7(), trackId, objectKey, contentType, expiresAt, createdAt);

    public bool IsConsumable(DateTimeOffset now) =>
        ConsumedAt is null && now <= ExpiresAt;

    public void Consume(DateTimeOffset now) => ConsumedAt = now;
}
