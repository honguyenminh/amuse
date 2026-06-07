using Amuse.Domain.Discovery;
using Amuse.Modules.Catalog.Contracts;
using Amuse.Modules.Media;

namespace Amuse.Modules.Discovery.Features.Common;

internal static class DiscoveryPlaylistCoverArt
{
    public const int MaxCovers = 3;

    public static async Task<IReadOnlyDictionary<Guid, string[]>> LoadAsync(
        IEnumerable<Playlist> playlists,
        ICatalogDiscoveryReadModel catalog,
        IMediaPublicUrlBuilder mediaUrls,
        CancellationToken cancellationToken)
    {
        var playlistList = playlists.ToList();
        if (playlistList.Count == 0)
            return new Dictionary<Guid, string[]>();

        var trackIds = playlistList
            .SelectMany(OrderedItemTrackIds)
            .Distinct()
            .ToArray();

        if (trackIds.Length == 0)
            return playlistList.ToDictionary(p => p.Id.Value, _ => Array.Empty<string>());

        var trackRows = await catalog.GetPlayableTrackRowsAsync(trackIds, cancellationToken);
        var releaseIds = trackRows.Values.Select(row => row.ReleaseId).Distinct().ToArray();
        var releases = releaseIds.Length > 0
            ? await catalog.GetReleaseSummariesAsync(releaseIds, cancellationToken)
            : new Dictionary<Guid, CatalogReleaseSummaryRow>();

        return playlistList.ToDictionary(
            playlist => playlist.Id.Value,
            playlist => ResolveCoverUrls(playlist, trackRows, releases, mediaUrls));
    }

    private static IEnumerable<Guid> OrderedItemTrackIds(Playlist playlist) =>
        playlist.Items
            .OrderBy(item => item.Position)
            .Take(MaxCovers * 4)
            .Select(item => item.TrackId.Value);

    private static string[] ResolveCoverUrls(
        Playlist playlist,
        IReadOnlyDictionary<Guid, CatalogTrackPlayableRow> trackRows,
        IReadOnlyDictionary<Guid, CatalogReleaseSummaryRow> releases,
        IMediaPublicUrlBuilder mediaUrls)
    {
        var urls = new List<string>(MaxCovers);
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var item in playlist.Items.OrderBy(i => i.Position))
        {
            if (urls.Count >= MaxCovers)
                break;

            if (!trackRows.TryGetValue(item.TrackId.Value, out var row))
                continue;

            if (!releases.TryGetValue(row.ReleaseId, out var release))
                continue;

            var url = DiscoveryMapper.CoverArtUrlForPublic(mediaUrls, release.CoverArtKey);
            if (string.IsNullOrEmpty(url) || !seen.Add(url))
                continue;

            urls.Add(url);
        }

        return urls.ToArray();
    }
}
