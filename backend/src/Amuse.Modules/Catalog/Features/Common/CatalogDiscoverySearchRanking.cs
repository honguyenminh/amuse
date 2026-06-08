using Amuse.Modules.Catalog.Contracts;

namespace Amuse.Modules.Catalog.Features.Common;

internal static class CatalogDiscoverySearchRanking
{
    internal static CatalogSearchResult RankAndPartition(
        IReadOnlyList<CatalogSearchItem> items,
        int limit)
    {
        var take = Math.Max(limit, 0);
        var ranked = items
            .OrderByDescending(item => item.IsVerified)
            .ThenBy(item => item.Title, StringComparer.OrdinalIgnoreCase)
            .Take(take)
            .ToList();

        return new CatalogSearchResult(
            ranked.Where(item => item.IsVerified).ToList(),
            ranked.Where(item => !item.IsVerified).ToList());
    }
}
