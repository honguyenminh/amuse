using Amuse.Modules.Catalog.Contracts;
using Amuse.Modules.Catalog.Features.Common;

namespace Amuse.Modules.Catalog.Tests;

public sealed class CatalogDiscoverySearchRankingTests
{
    [Fact]
    public void RankAndPartition_prefers_verified_items_within_limit()
    {
        var items = new[]
        {
            CreateItem("Unverified Zebra", isVerified: false),
            CreateItem("Verified Alpha", isVerified: true),
            CreateItem("Unverified Alpha", isVerified: false),
            CreateItem("Verified Zebra", isVerified: true),
            CreateItem("Unverified Beta", isVerified: false),
        };

        var result = CatalogDiscoverySearchRanking.RankAndPartition(items, limit: 3);

        Assert.Equal(2, result.Verified.Count);
        Assert.Collection(
            result.Verified,
            item => Assert.Equal("Verified Alpha", item.Title),
            item => Assert.Equal("Verified Zebra", item.Title));
        Assert.Single(result.Unverified);
        Assert.Equal("Unverified Alpha", result.Unverified[0].Title);
    }

    [Fact]
    public void RankAndPartition_returns_only_verified_when_limit_filled_by_verified()
    {
        var items = new[]
        {
            CreateItem("Verified One", isVerified: true),
            CreateItem("Verified Two", isVerified: true),
            CreateItem("Unverified One", isVerified: false),
        };

        var result = CatalogDiscoverySearchRanking.RankAndPartition(items, limit: 2);

        Assert.Equal(2, result.Verified.Count);
        Assert.Empty(result.Unverified);
    }

    private static CatalogSearchItem CreateItem(string title, bool isVerified) =>
        new(
            CatalogSearchItemKind.Release,
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
}
