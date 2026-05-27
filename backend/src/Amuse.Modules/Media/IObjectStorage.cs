namespace Amuse.Modules.Media;

public enum MediaBucket
{
    Covers = 1,
    Audio = 2,
}

public sealed record StoredObject(ReadOnlyMemory<byte> Data, string ContentType);

/// <summary>
/// Storage abstraction for media objects. Implementations target an S3-compatible
/// backend (MinIO in dev, AWS S3 / Backblaze B2 / etc. in prod).
/// </summary>
public interface IObjectStorage
{
    /// <summary>
    /// Returns true if an object with <paramref name="key"/> exists in the bucket.
    /// </summary>
    Task<bool> ObjectExistsAsync(MediaBucket bucket, string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Uploads bytes to <paramref name="key"/>. Overwrites if the object already exists.
    /// </summary>
    Task PutAsync(
        MediaBucket bucket,
        string key,
        ReadOnlyMemory<byte> data,
        string contentType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads an object from storage; returns null when the key does not exist.
    /// </summary>
    Task<StoredObject?> GetAsync(
        MediaBucket bucket,
        string key,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the anonymous-read URL for an object in a public bucket. Throws if the
    /// bucket is not configured for anonymous reads (covers only).
    /// </summary>
    string GetPublicUrl(MediaBucket bucket, string key);

    /// <summary>
    /// Returns a time-limited presigned GET URL for an object. Suitable for audio
    /// streams that must not be guessable from the URL pattern.
    /// </summary>
    string GetSignedUrl(MediaBucket bucket, string key, TimeSpan ttl);

    /// <summary>
    /// Returns a time-limited presigned PUT URL for uploading an object directly from a client.
    /// Intended for large masters (audio/video) so the API does not proxy bytes.
    /// </summary>
    string GetSignedUploadUrl(MediaBucket bucket, string key, TimeSpan ttl, string contentType);
}
