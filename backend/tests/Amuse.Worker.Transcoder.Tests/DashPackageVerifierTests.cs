using Amuse.Modules.Media;
using Amuse.Worker.Transcoder;
using Xunit;

namespace Amuse.Worker.Transcoder.Tests;

public sealed class DashPackageVerifierTests
{
    [Fact]
    public async Task IsCompleteAsync_returns_false_when_only_manifest_exists()
    {
        var storage = new FakeObjectStorage(
        [
            "dash/track/manifest-id/manifest.mpd",
        ]);

        var result = await DashPackageVerifier.IsCompleteAsync(
            storage,
            "dash/track/manifest-id/manifest.mpd",
            CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task IsCompleteAsync_returns_true_when_manifest_and_segments_exist()
    {
        var storage = new FakeObjectStorage(
        [
            "dash/track/manifest-id/manifest.mpd",
            "dash/track/manifest-id/init-stream0.m4s",
            "dash/track/manifest-id/chunk-stream0-00001.m4s",
        ]);

        var result = await DashPackageVerifier.IsCompleteAsync(
            storage,
            "dash/track/manifest-id/manifest.mpd",
            CancellationToken.None);

        Assert.True(result);
    }

    private sealed class FakeObjectStorage : IObjectStorage
    {
        private readonly HashSet<string> _keys;

        public FakeObjectStorage(IEnumerable<string> keys) =>
            _keys = keys.ToHashSet(StringComparer.Ordinal);

        public Task<bool> ObjectExistsAsync(MediaBucket bucket, string key, CancellationToken cancellationToken = default) =>
            Task.FromResult(_keys.Contains(key));

        public Task PutAsync(MediaBucket bucket, string key, ReadOnlyMemory<byte> data, string contentType, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<StoredObject?> GetAsync(MediaBucket bucket, string key, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public string GetPublicUrl(MediaBucket bucket, string key) => throw new NotSupportedException();

        public string GetSignedUrl(MediaBucket bucket, string key, TimeSpan ttl) => throw new NotSupportedException();

        public string GetInternalSignedUrl(MediaBucket bucket, string key, TimeSpan ttl) => throw new NotSupportedException();

        public string GetSignedUploadUrl(MediaBucket bucket, string key, TimeSpan ttl, string contentType) =>
            throw new NotSupportedException();

        public Task DeleteAsync(MediaBucket bucket, string key, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task DeleteByPrefixAsync(MediaBucket bucket, string prefix, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<string>> ListByPrefixAsync(
            MediaBucket bucket,
            string prefix,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<string>>(_keys.Where(k => k.StartsWith(prefix, StringComparison.Ordinal)).ToList());
    }
}
