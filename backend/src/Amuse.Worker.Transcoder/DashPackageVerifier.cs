using Amuse.Modules.Media;

namespace Amuse.Worker.Transcoder;

public static class DashPackageVerifier
{
    public static async Task<bool> IsCompleteAsync(
        IObjectStorage storage,
        string dashManifestKey,
        CancellationToken cancellationToken)
    {
        if (!await storage.ObjectExistsAsync(MediaBucket.Audio, dashManifestKey, cancellationToken))
            return false;

        var prefix = GetDashPrefix(dashManifestKey);
        var keys = await storage.ListByPrefixAsync(MediaBucket.Audio, prefix, cancellationToken);

        var hasInit = keys.Any(k => Path.GetFileName(k).StartsWith("init-stream", StringComparison.Ordinal)
            && k.EndsWith(".m4s", StringComparison.OrdinalIgnoreCase));
        var hasChunk = keys.Any(k => Path.GetFileName(k).StartsWith("chunk-stream", StringComparison.Ordinal)
            && k.EndsWith(".m4s", StringComparison.OrdinalIgnoreCase));

        return hasInit && hasChunk;
    }

    private static string GetDashPrefix(string streamKey)
    {
        var lastSlash = streamKey.LastIndexOf('/');
        return lastSlash >= 0 ? streamKey[..(lastSlash + 1)] : streamKey;
    }
}
