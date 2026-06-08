using Amuse.Modules.Discovery.Features.Search;

namespace Amuse.Modules.Discovery.Tests;

public sealed class DiscoverySearchKindsTests
{
    [Fact]
    public void Parse_returns_null_for_empty_or_all_kinds()
    {
        Assert.Null(DiscoverySearchKinds.Parse(null));
        Assert.Null(DiscoverySearchKinds.Parse([]));
        Assert.Null(DiscoverySearchKinds.Parse(
        [
            "artist",
            "release",
            "track",
            "playlist",
        ]));
    }

    [Fact]
    public void Parse_returns_subset_for_partial_selection()
    {
        var parsed = DiscoverySearchKinds.Parse(["artist", "track"]);

        Assert.NotNull(parsed);
        Assert.Equal(2, parsed!.Count);
        Assert.Contains(DiscoverySearchKinds.Artist, parsed);
        Assert.Contains(DiscoverySearchKinds.Track, parsed);
    }

    [Fact]
    public void Parse_supports_comma_separated_values()
    {
        var parsed = DiscoverySearchKinds.Parse(["artist,playlist"]);

        Assert.NotNull(parsed);
        Assert.Equal(2, parsed!.Count);
        Assert.Contains(DiscoverySearchKinds.Artist, parsed);
        Assert.Contains(DiscoverySearchKinds.Playlist, parsed);
    }
}
