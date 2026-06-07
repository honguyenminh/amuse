using System.Security.Claims;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Catalog.Contracts;
using Amuse.Modules.Discovery.Features.Common;
using Amuse.Modules.Identity.Contracts;
using Amuse.Modules.Media;

namespace Amuse.Modules.Discovery.Features.ListLibraryLiked;

internal sealed class ListLibraryLikedHandler(
    ICatalogDiscoveryReadModel catalog,
    IListenerPersonaReadModel personaReadModel,
    IMediaPublicUrlBuilder mediaUrls,
    LikedPlaylistLoader likedPlaylistLoader)
{
    public async Task<Result<LikedTracksResponse>> HandleAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var listenerResult = await DiscoveryPrincipal.RequireListenerAsync(
            principal, personaReadModel, cancellationToken);
        if (!listenerResult.IsSuccess)
            return Result<LikedTracksResponse>.Failure(listenerResult.Error!);

        var playlist = await likedPlaylistLoader.GetForReadAsync(
            listenerResult.Value!.ListenerProfileId,
            cancellationToken);
        if (playlist is null || playlist.Items.Count == 0)
            return Result<LikedTracksResponse>.Success(new LikedTracksResponse([]));

        var orderedItems = playlist.Items.OrderBy(i => i.Position).ToArray();
        var trackIds = orderedItems.Select(i => i.TrackId.Value).ToArray();
        var rows = await catalog.GetPlayableTrackRowsAsync(trackIds, cancellationToken);
        var releaseIds = rows.Values.Select(r => r.ReleaseId).Distinct().ToArray();
        var releaseSummaries = await catalog.GetReleaseSummariesAsync(releaseIds, cancellationToken);

        var tracks = orderedItems
            .Where(i => rows.ContainsKey(i.TrackId.Value))
            .Select(i =>
            {
                var row = rows[i.TrackId.Value];
                releaseSummaries.TryGetValue(row.ReleaseId, out var release);
                return new LikedTrackRowDto(
                    row.TrackId,
                    row.Title,
                    row.DurationMs,
                    row.HasAudio,
                    DiscoveryMapper.CoverArtUrlForPublic(mediaUrls, release?.CoverArtKey),
                    row.ReleaseId,
                    row.ReleaseTitle,
                    row.ArtistName,
                    i.AddedAt);
            })
            .ToArray();

        return Result<LikedTracksResponse>.Success(new LikedTracksResponse(tracks));
    }
}
