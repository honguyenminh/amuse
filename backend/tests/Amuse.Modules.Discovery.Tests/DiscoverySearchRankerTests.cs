using Amuse.Domain.Discovery;
using Amuse.Domain.Listener;
using Amuse.Modules.Catalog.Contracts;
using Amuse.Modules.Discovery.Features.Search;

namespace Amuse.Modules.Discovery.Tests;

public sealed class DiscoverySearchRankerTests
{
    [Fact]
    public void RankMixed_orders_by_relevance_not_verified_first()
    {
        var items = new[]
        {
            CreateCatalogItem("My Alpha Song", isVerified: true),
            CreateCatalogItem("Alpha", isVerified: false),
            CreateCatalogItem("Alpha Verified", isVerified: true),
        };

        var ranked = DiscoverySearchRanker.RankMixed(
            "alpha",
            items,
            [],
            allowUnverifiedArtists: false,
            kindFilter: null,
            limit: 3);

        Assert.Collection(
            ranked,
            first => Assert.Equal("Alpha", Assert.IsType<CatalogSearchItem>(first).Title),
            second => Assert.Equal("Alpha Verified", Assert.IsType<CatalogSearchItem>(second).Title),
            third => Assert.Equal("My Alpha Song", Assert.IsType<CatalogSearchItem>(third).Title));
    }

    [Fact]
    public void RankMixed_interleaves_playlists_by_score()
    {
        var items = new[] { CreateCatalogItem("Alpha Release", isVerified: true) };
        var playlists = new[]
        {
            CreatePlaylist("Alphabet Mix"),
            CreatePlaylist("Alpha Playlist"),
        };

        var ranked = DiscoverySearchRanker.RankMixed(
            "alpha",
            items,
            playlists,
            allowUnverifiedArtists: false,
            kindFilter: null,
            limit: 3);

        Assert.Equal(3, ranked.Count);
        Assert.Equal("Alpha Playlist", Assert.IsType<Playlist>(ranked[0]).Title);
        Assert.Equal("Alpha Release", Assert.IsType<CatalogSearchItem>(ranked[1]).Title);
        Assert.Equal("Alphabet Mix", Assert.IsType<Playlist>(ranked[2]).Title);
    }

    [Fact]
    public void RankMixed_filters_by_kind()
    {
        var items = new[]
        {
            CreateCatalogItem("Alpha Artist", isVerified: true, kind: CatalogSearchItemKind.Artist),
            CreateCatalogItem("Alpha Release", isVerified: true, kind: CatalogSearchItemKind.Release),
        };

        var ranked = DiscoverySearchRanker.RankMixed(
            "alpha",
            items,
            [],
            allowUnverifiedArtists: false,
            kindFilter: new HashSet<string> { DiscoverySearchKinds.Release },
            limit: 5);

        var single = Assert.Single(ranked);
        Assert.Equal("Alpha Release", Assert.IsType<CatalogSearchItem>(single).Title);
    }

    private static CatalogSearchItem CreateCatalogItem(
        string title,
        bool isVerified,
        CatalogSearchItemKind kind = CatalogSearchItemKind.Release) =>
        new(
            kind,
            Guid.CreateVersion7(),
            title,
            null,
            "artist-slug",
            "release-slug",
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            null,
            isVerified ? "platformVerified" : "unverified",
            isVerified);

    private static Playlist CreatePlaylist(string title)
    {
        var ownerId = ListenerProfileId.From(Guid.CreateVersion7());
        return Playlist.CreateOwned(
            ownerId,
            title,
            PlaylistVisibility.Public,
            DateTimeOffset.UtcNow).Value!;
    }
}
