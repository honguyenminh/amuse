using System.Security.Claims;
using Amuse.Domain.Discovery;
using Amuse.Domain.Listener;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Catalog.Contracts;
using Amuse.Modules.Common.Time;
using Amuse.Modules.Discovery.Features.Shared;
using Amuse.Modules.Discovery.Persistence;
using Amuse.Modules.Identity.Contracts;
using Amuse.Modules.Listener.Contracts;
using Amuse.Modules.Media;

namespace Amuse.Modules.Discovery.Features.GetLikedPlaylist;

internal sealed class GetLikedPlaylistHandler(
    DiscoveryDbContext db,
    ICatalogDiscoveryReadModel catalog,
    IListenerPersonaReadModel personaReadModel,
    IListenerProfilePresentationReadModel presentationReadModel,
    IObjectStorage storage,
    IClock clock)
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
        var playlist = await LikedPlaylistService.GetOrCreateForMutationAsync(
            db,
            listenerId,
            clock.UtcNow,
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
                return DiscoveryMapper.ToItemDto(i, row, storage, release?.CoverArtKey);
            })
            .ToArray();

        var owners = await DiscoveryMapper.LoadOwnersAsync(
            [playlist.OwnerListenerProfileId],
            presentationReadModel,
            storage,
            cancellationToken);
        owners.TryGetValue(playlist.OwnerListenerProfileId.Value, out var owner);

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
