using System.Security.Claims;
using Amuse.Domain.Discovery;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Catalog.Contracts;
using Amuse.Modules.Discovery.Features.Shared;
using Amuse.Modules.Discovery.Persistence;
using Amuse.Modules.Identity.Contracts;
using Amuse.Modules.Listener.Contracts;
using Amuse.Modules.Media;

namespace Amuse.Modules.Discovery.Features.GetPlaylist;

internal sealed class GetPlaylistHandler(
    DiscoveryDbContext db,
    ICatalogDiscoveryReadModel catalog,
    IListenerPersonaReadModel personaReadModel,
    IListenerProfilePresentationReadModel presentationReadModel,
    PlaylistViewContextBuilder viewContextBuilder,
    PlaylistLoader playlistLoader,
    IMediaPublicUrlBuilder mediaUrls)
{
    public async Task<Result<PlaylistDetailDto>> HandleAsync(
        Guid playlistId,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        if (playlistId == Guid.Empty)
            return Result<PlaylistDetailDto>.Failure(DiscoveryErrors.PlaylistNotFound);

        var playlist = await playlistLoader.GetForReadAsync(
            PlaylistId.From(playlistId), cancellationToken);
        if (playlist is null)
            return Result<PlaylistDetailDto>.Failure(DiscoveryErrors.PlaylistNotFound);

        var viewContext = await viewContextBuilder.BuildAsync(principal, cancellationToken);
        if (!playlist.CanBeViewedBy(viewContext))
            return Result<PlaylistDetailDto>.Failure(DiscoveryErrors.PlaylistForbidden);

        var viewerProfileId = await DiscoveryPrincipal.TryResolveListenerProfileIdAsync(
            principal, personaReadModel, cancellationToken);

        var engagement = await DiscoveryEngagementQueries.GetPlaylistEngagementAsync(
            db,
            viewerProfileId,
            [playlistId],
            cancellationToken);

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

        var owners = await DiscoveryMapper.LoadOwnersAsync(
            [playlist.OwnerListenerProfileId],
            presentationReadModel,
            mediaUrls,
            cancellationToken);
        owners.TryGetValue(playlist.OwnerListenerProfileId.Value, out var owner);

        var isOwner = viewerProfileId is not null && playlist.OwnerListenerProfileId == viewerProfileId;
        return Result<PlaylistDetailDto>.Success(
            DiscoveryMapper.ToDetail(
                playlist,
                items,
                owner,
                viewerProfileId,
                engagement,
                includeShareEmails: isOwner));
    }
}
