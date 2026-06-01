using System.Collections.Concurrent;
using Amuse.Modules.Media;

namespace Amuse.Api.IntegrationTests;

/// <summary>
/// Drop-in <see cref="IObjectStorage"/> for integration tests. Stores blobs in-process so
/// tests don't need a real MinIO. Public/signed URLs are deterministic and unique per key
/// so the catalog endpoints can be asserted against them.
/// </summary>
public sealed class InMemoryObjectStorage : IObjectStorage
{
    private readonly ConcurrentDictionary<(MediaBucket, string), Entry> _objects = new();

    public Task<bool> ObjectExistsAsync(MediaBucket bucket, string key, CancellationToken cancellationToken = default) =>
        Task.FromResult(_objects.ContainsKey((bucket, key)));

    public Task PutAsync(
        MediaBucket bucket,
        string key,
        ReadOnlyMemory<byte> data,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        _objects[(bucket, key)] = new Entry(data.ToArray(), contentType);
        return Task.CompletedTask;
    }

    public Task<StoredObject?> GetAsync(
        MediaBucket bucket,
        string key,
        CancellationToken cancellationToken = default)
    {
        if (!_objects.TryGetValue((bucket, key), out var entry))
            return Task.FromResult<StoredObject?>(null);
        return Task.FromResult<StoredObject?>(new StoredObject(entry.Bytes, entry.ContentType));
    }

    public string GetPublicUrl(MediaBucket bucket, string key) =>
        bucket == MediaBucket.Covers
            ? $"https://test.media.amuse.local/{BucketName(bucket)}/{Uri.EscapeDataString(key)}"
            : throw new InvalidOperationException("Audio bucket is not public.");

    public string GetSignedUrl(MediaBucket bucket, string key, TimeSpan ttl) =>
        BuildSignedGetUrl(bucket, key, ttl, "public");

    public string GetInternalSignedUrl(MediaBucket bucket, string key, TimeSpan ttl) =>
        BuildSignedGetUrl(bucket, key, ttl, "internal");

    public string GetSignedUploadUrl(MediaBucket bucket, string key, TimeSpan ttl, string contentType) =>
        $"https://test.media.amuse.local/{BucketName(bucket)}/{Uri.EscapeDataString(key)}" +
        $"?X-Amz-Signature=test&X-Amz-Expires={(int)ttl.TotalSeconds}&X-Amz-Verb=PUT&ContentType={Uri.EscapeDataString(contentType)}";

    public Task DeleteAsync(MediaBucket bucket, string key, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(key))
            _objects.TryRemove((bucket, key), out _);
        return Task.CompletedTask;
    }

    public Task DeleteByPrefixAsync(MediaBucket bucket, string prefix, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(prefix))
            return Task.CompletedTask;

        foreach (var entry in _objects.Keys.Where(k => k.Item1 == bucket && k.Item2.StartsWith(prefix, StringComparison.Ordinal)))
            _objects.TryRemove(entry, out _);

        return Task.CompletedTask;
    }

    public bool Contains(MediaBucket bucket, string key) => _objects.ContainsKey((bucket, key));

    private static string BuildSignedGetUrl(MediaBucket bucket, string key, TimeSpan ttl, string audience) =>
        $"https://test.media.amuse.local/{BucketName(bucket)}/{Uri.EscapeDataString(key)}" +
        $"?X-Amz-Signature=test&X-Amz-Expires={(int)ttl.TotalSeconds}&audience={audience}";

    private static string BucketName(MediaBucket bucket) => bucket switch
    {
        MediaBucket.Covers => "amuse-covers",
        MediaBucket.Audio => "amuse-audio",
        _ => throw new ArgumentOutOfRangeException(nameof(bucket), bucket, null),
    };

    private sealed record Entry(byte[] Bytes, string ContentType);
}
