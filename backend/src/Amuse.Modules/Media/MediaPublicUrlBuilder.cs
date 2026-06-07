namespace Amuse.Modules.Media;

internal sealed class MediaPublicUrlBuilder(IObjectStorage storage) : IMediaPublicUrlBuilder
{
    public string? BuildCoverArtUrl(string? objectKey) =>
        string.IsNullOrEmpty(objectKey)
            ? null
            : storage.GetPublicUrl(MediaBucket.Covers, objectKey);
}
