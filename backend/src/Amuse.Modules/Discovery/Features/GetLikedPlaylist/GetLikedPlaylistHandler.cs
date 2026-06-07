using System.Security.Claims;
using Amuse.Domain.Discovery;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Catalog.Contracts;
using Amuse.Modules.Discovery.Features.Common;
using Amuse.Modules.Identity.Contracts;
using Amuse.Modules.Listener.Contracts;
using Amuse.Modules.Media;

namespace Amuse.Modules.Discovery.Features.GetLikedPlaylist;

internal sealed class GetLikedPlaylistHandler(
    ICatalogDiscoveryReadModel catalog,
    IListenerPersonaReadModel personaReadModel,
    IListenerProfilePresentationReadModel presentationReadModel,
    IMediaPublicUrlBuilder mediaUrls,
    LikedPlaylistLoader likedPlaylistLoader)
{
    public async Task<Result<PlaylistDetailDto>> HandleAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var listenerResult = await DiscoveryPrincipal.RequireListenerAsync(
            principal, personaReadModel, cancellationToken);
        if (!listenerResult.IsSuccess)
            return Result<PlaylistDetailDto>.Failure(listenerResult.Error!);

        var listenerId = listenerResult.Value!.ListenerProfileId;
        var playlist = await likedPlaylistLoader.GetForReadAsync(listenerId, cancellationToken);

        var owners = await DiscoveryMapper.LoadOwnersAsync(
            [listenerId],
            presentationReadModel,
            mediaUrls,
            cancellationToken);
        owners.TryGetValue(listenerId.Value, out var owner);

        if (playlist is null)
        {
            return Result<PlaylistDetailDto>.Success(
                new PlaylistDetailDto(
                    Guid.Empty,
                    Playlist.LikedCollectionTitle,
                    DiscoveryKind.ToApiValue(PlaylistKind.Liked),
                    null,
                    DiscoveryVisibility.ToApiValue(PlaylistVisibility.Private),
                    owner,
                    null,
                    [],
                    null,
                    DateTimeOffset.MinValue,
                    DateTimeOffset.MinValue,
                    true,
                    false,
                    false,
                    false));
        }

        var trackIds = playlist.Items.OrderBy(i => i.Position).Select(i => i.TrackId.Value).ToArray();
        var trackRows = trackIds.Length > 0
            ? await catalog.GetPlayableTrackRowsAsync(trackIds, cancellationToken)
            : new Dictionary<Guid, CatalogTrackPlayableRow>();

        var releaseIds = trackRows.Values.Select(r => r.ReleaseId).Distinct().ToArray();
        var releaseSummaries = releaseIds.Length > 0
            ? await catalog.GetReleaseSummariesAsync(releaseIds, cancellationToken)
            : new Dictionary<Guid, CatalogReleaseSummaryRow>();

        var items = playlist.Items
            .OrderBy(i => i.Position)
            .Where(i => trackRows.ContainsKey(i.TrackId.Value))
            .Select(i =>
            {
                var row = trackRows[i.TrackId.Value];
                releaseSummaries.TryGetValue(row.ReleaseId, out var release);
                return DiscoveryMapper.ToItemDto(i, row, mediaUrls, release?.CoverArtKey);
            })
            .ToArray();

        return Result<PlaylistDetailDto>.Success(
            DiscoveryMapper.ToDetail(
                playlist,
                items,
                owner,
                listenerId,
                null,
                includeShareEmails: true));
    }
}
