using Amuse.Domain.Discovery;
using Amuse.Modules.Catalog.Contracts;

namespace Amuse.Modules.Discovery.Features.Search;

internal sealed record RankedCatalogSearchItem(
    CatalogSearchItem Item,
    int Score);

internal sealed record RankedPlaylistSearchItem(
    Playlist Playlist,
    int Score);

internal static class DiscoverySearchRanker
{
    internal static IReadOnlyList<RankedCatalogSearchItem> RankCatalogItems(
        string query,
        IReadOnlyList<CatalogSearchItem> items,
        bool? allowUnverifiedArtists,
        IReadOnlySet<string>? kindFilter,
        int limit)
    {
        var normalizedQuery = query.Trim();

        return items
            .Select(item => new RankedCatalogSearchItem(
                item,
                DiscoverySearchScoring.ComputeFinalScore(
                    normalizedQuery,
                    ToMatchCandidate(item),
                    allowUnverifiedArtists)))
            .Where(entry => entry.Score > 0)
            .Where(entry => MatchesKindFilter(entry.Item, kindFilter))
            .OrderByDescending(entry => entry.Score)
            .ThenBy(entry => entry.Item.Title, StringComparer.OrdinalIgnoreCase)
            .Take(limit)
            .ToList();
    }

    internal static IReadOnlyList<RankedPlaylistSearchItem> RankPlaylists(
        string query,
        IReadOnlyList<Playlist> playlists,
        bool? allowUnverifiedArtists,
        IReadOnlySet<string>? kindFilter,
        int limit)
    {
        if (!DiscoverySearchKinds.IncludesPlaylist(kindFilter))
            return [];

        var normalizedQuery = query.Trim();

        return playlists
            .Select(playlist => new RankedPlaylistSearchItem(
                playlist,
                DiscoverySearchScoring.ComputeFinalScore(
                    normalizedQuery,
                    new DiscoverySearchScoring.SearchMatchCandidate(
                        DiscoverySearchKinds.Playlist,
                        playlist.Title,
                        [],
                        IsVerified: true),
                    allowUnverifiedArtists)))
            .Where(entry => entry.Score > 0)
            .OrderByDescending(entry => entry.Score)
            .ThenBy(entry => entry.Playlist.Title, StringComparer.OrdinalIgnoreCase)
            .Take(limit)
            .ToList();
    }

    internal static IReadOnlyList<object> RankMixed(
        string query,
        IReadOnlyList<CatalogSearchItem> catalogItems,
        IReadOnlyList<Playlist> playlists,
        bool? allowUnverifiedArtists,
        IReadOnlySet<string>? kindFilter,
        int limit)
    {
        var normalizedQuery = query.Trim();
        var entries = new List<(object Payload, string Kind, string Title, int Score)>();

        foreach (var item in catalogItems)
        {
            if (!MatchesKindFilter(item, kindFilter))
                continue;

            var score = DiscoverySearchScoring.ComputeFinalScore(
                normalizedQuery,
                ToMatchCandidate(item),
                allowUnverifiedArtists);

            if (score <= 0)
                continue;

            entries.Add((item, item.Kind.ToString().ToLowerInvariant(), item.Title, score));
        }

        if (DiscoverySearchKinds.IncludesPlaylist(kindFilter))
        {
            foreach (var playlist in playlists)
            {
                var score = DiscoverySearchScoring.ComputeFinalScore(
                    normalizedQuery,
                    new DiscoverySearchScoring.SearchMatchCandidate(
                        DiscoverySearchKinds.Playlist,
                        playlist.Title,
                        [],
                        IsVerified: true),
                    allowUnverifiedArtists);

                if (score <= 0)
                    continue;

                entries.Add((playlist, DiscoverySearchKinds.Playlist, playlist.Title, score));
            }
        }

        return entries
            .OrderByDescending(entry => entry.Score)
            .ThenBy(entry => entry.Title, StringComparer.OrdinalIgnoreCase)
            .Take(limit)
            .Select(entry => entry.Payload)
            .ToList();
    }

    private static bool MatchesKindFilter(CatalogSearchItem item, IReadOnlySet<string>? kindFilter)
    {
        if (kindFilter is null)
            return true;

        var kind = item.Kind switch
        {
            CatalogSearchItemKind.Artist => DiscoverySearchKinds.Artist,
            CatalogSearchItemKind.Release => DiscoverySearchKinds.Release,
            CatalogSearchItemKind.Track => DiscoverySearchKinds.Track,
            _ => string.Empty,
        };

        return kindFilter.Contains(kind);
    }

    private static DiscoverySearchScoring.SearchMatchCandidate ToMatchCandidate(CatalogSearchItem item) =>
        item.Kind switch
        {
            CatalogSearchItemKind.Artist => new DiscoverySearchScoring.SearchMatchCandidate(
                DiscoverySearchKinds.Artist,
                item.Title,
                SlugList(item.ArtistSlug),
                item.IsVerified),
            CatalogSearchItemKind.Release => new DiscoverySearchScoring.SearchMatchCandidate(
                DiscoverySearchKinds.Release,
                item.Title,
                SlugList(item.ReleaseSlug),
                item.IsVerified),
            CatalogSearchItemKind.Track => new DiscoverySearchScoring.SearchMatchCandidate(
                DiscoverySearchKinds.Track,
                item.Title,
                SlugList(item.ArtistSlug, item.ReleaseSlug),
                item.IsVerified),
            _ => new DiscoverySearchScoring.SearchMatchCandidate(
                item.Kind.ToString().ToLowerInvariant(),
                item.Title,
                [],
                item.IsVerified),
        };

    private static IReadOnlyList<string> SlugList(params string?[] slugs) =>
        slugs.Where(slug => !string.IsNullOrWhiteSpace(slug)).Select(slug => slug!).ToArray();
}
