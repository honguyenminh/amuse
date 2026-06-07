using Amuse.Domain.Catalog;
using Amuse.Domain.Discovery;
using Amuse.Domain.Listener;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Catalog.Contracts;
using Amuse.Modules.Media;

namespace Amuse.Modules.Discovery.Features.Common;

internal sealed class PlayableCollectionResolver(
    PlaylistLoader playlistLoader,
    LikedPlaylistLoader likedPlaylistLoader,
    ICatalogDiscoveryReadModel catalog,
    IMediaPublicUrlBuilder mediaUrls)
{
    public async Task<Result<PlayableTracksResponse>> ResolvePlaylistTracksAsync(
        PlaylistId playlistId,
        PlaylistViewContext viewContext,
        CancellationToken cancellationToken)
    {
        var playlist = await playlistLoader.GetForReadAsync(playlistId, cancellationToken);

        if (playlist is null)
            return Result<PlayableTracksResponse>.Failure(DiscoveryErrors.PlaylistNotFound);

        if (!playlist.CanBeViewedBy(viewContext))
            return Result<PlayableTracksResponse>.Failure(DiscoveryErrors.PlaylistForbidden);

        var trackIds = playlist.Items
            .OrderBy(i => i.Position)
            .Select(i => i.TrackId.Value)
            .ToArray();

        if (trackIds.Length == 0)
            return Result<PlayableTracksResponse>.Success(new PlayableTracksResponse([]));

        var rows = await catalog.GetPlayableTrackRowsAsync(trackIds, cancellationToken);
        var releaseIds = rows.Values.Select(r => r.ReleaseId).Distinct().ToArray();
        var releaseSummaries = await catalog.GetReleaseSummariesAsync(releaseIds, cancellationToken);

        var tracks = trackIds
            .Where(rows.ContainsKey)
            .Select(id =>
            {
                var row = rows[id];
                releaseSummaries.TryGetValue(row.ReleaseId, out var release);
                return DiscoveryMapper.ToPlayableTrack(row, mediaUrls, release?.CoverArtKey);
            })
            .ToArray();

        return Result<PlayableTracksResponse>.Success(new PlayableTracksResponse(tracks));
    }

    public async Task<Result<PlayableTracksResponse>> ResolveLikedTracksAsync(
        ListenerProfileId listenerId,
        CancellationToken cancellationToken)
    {
        var playlist = await likedPlaylistLoader.GetForReadAsync(listenerId, cancellationToken);
        if (playlist is null)
            return Result<PlayableTracksResponse>.Success(new PlayableTracksResponse([]));

        return await ResolvePlaylistTracksAsync(
            playlist.Id,
            new PlaylistViewContext(listenerId, null),
            cancellationToken);
    }

    public async Task<Result<PlayableTracksResponse>> ResolveReleaseTracksAsync(
        Guid releaseId,
        CancellationToken cancellationToken)
    {
        if (releaseId == Guid.Empty)
            return Result<PlayableTracksResponse>.Failure(DiscoveryErrors.InvalidReleaseId);

        var release = ReleaseId.From(releaseId);
        if (!await catalog.ReleaseExistsAndPublishedAsync(release, cancellationToken))
            return Result<PlayableTracksResponse>.Failure(DiscoveryErrors.InvalidReleaseId);

        var rows = await catalog.GetPlayableTracksForReleaseAsync(release, cancellationToken);
        var releaseSummaries = await catalog.GetReleaseSummariesAsync([releaseId], cancellationToken);
        releaseSummaries.TryGetValue(releaseId, out var summary);

        var tracks = rows
            .Select(row => DiscoveryMapper.ToPlayableTrack(row, mediaUrls, summary?.CoverArtKey))
            .ToArray();

        return Result<PlayableTracksResponse>.Success(new PlayableTracksResponse(tracks));
    }
}
