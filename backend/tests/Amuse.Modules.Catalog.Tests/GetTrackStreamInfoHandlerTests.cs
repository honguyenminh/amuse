using Amuse.Domain.Catalog;
using Amuse.Modules.Catalog.Features.GetTrackStreamInfo;

namespace Amuse.Modules.Catalog.Tests;

public sealed class GetTrackStreamInfoHandlerTests
{
    [Fact]
    public void IsPubliclyStreamable_requires_published_track_and_release()
    {
        Assert.True(GetTrackStreamInfoHandler.IsPubliclyStreamable(
            TrackLifecycleStatus.Published,
            ReleaseLifecycleStatus.Published));

        Assert.False(GetTrackStreamInfoHandler.IsPubliclyStreamable(
            TrackLifecycleStatus.Draft,
            ReleaseLifecycleStatus.Published));

        Assert.False(GetTrackStreamInfoHandler.IsPubliclyStreamable(
            TrackLifecycleStatus.Published,
            ReleaseLifecycleStatus.Hidden));
    }

    [Fact]
    public void FilterRenditionsForPublicPreview_caps_at_128kbps_and_excludes_flac()
    {
        var renditions = new[]
        {
            new TrackStreamRenditionDto("flac-0", "flac", null, 800_000, 48_000, "flac", "0"),
            new TrackStreamRenditionDto("opus-256", "opus", 256, 256_000, 48_000, "opus", "3"),
            new TrackStreamRenditionDto("opus-128", "opus", 128, 128_000, 48_000, "opus", "2"),
            new TrackStreamRenditionDto("opus-64", "opus", 64, 64_000, 48_000, "opus", "1"),
        };

        var filtered = GetTrackStreamInfoHandler.FilterRenditionsForPublicPreview(renditions);

        Assert.Equal(2, filtered.Count);
        Assert.Contains(filtered, r => r.Id == "opus-128");
        Assert.Contains(filtered, r => r.Id == "opus-64");
        Assert.DoesNotContain(filtered, r => r.Codec == "flac");
        Assert.DoesNotContain(filtered, r => r.BitrateKbps > 128);
    }

    [Fact]
    public void FilterRenditionsForPublicPreview_falls_back_to_legacy_aac_128()
    {
        var renditions = new[]
        {
            new TrackStreamRenditionDto("flac-0", "flac", null, 800_000, 48_000, "flac", "0"),
            new TrackStreamRenditionDto("opus-256", "opus", 256, 256_000, 48_000, "opus", "3"),
        };

        var filtered = GetTrackStreamInfoHandler.FilterRenditionsForPublicPreview(renditions);

        Assert.Single(filtered);
        Assert.Equal("aac-128", filtered[0].Id);
    }
}
