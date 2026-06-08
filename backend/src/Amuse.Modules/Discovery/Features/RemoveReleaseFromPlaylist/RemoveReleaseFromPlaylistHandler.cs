using System.Security.Claims;
using Amuse.Domain.Catalog;
using Amuse.Domain.Discovery;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Catalog.Contracts;
using Amuse.Modules.Common.Time;
using Amuse.Modules.Discovery.Features.Common;
using Amuse.Modules.Discovery.Persistence;
using Amuse.Modules.Identity.Contracts;

namespace Amuse.Modules.Discovery.Features.RemoveReleaseFromPlaylist;

internal sealed class RemoveReleaseFromPlaylistHandler(
    DiscoveryDbContext db,
    ICatalogDiscoveryReadModel catalog,
    IListenerPersonaReadModel personaReadModel,
    PlaylistLoader playlistLoader,
    IClock clock)
{
    public async Task<Result> HandleAsync(
        Guid playlistId,
        Guid releaseId,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        if (playlistId == Guid.Empty)
            return Result.Failure(DiscoveryErrors.PlaylistNotFound);

        if (releaseId == Guid.Empty)
            return Result.Failure(DiscoveryErrors.InvalidReleaseId);

        var listenerResult = await DiscoveryPrincipal.RequireListenerAsync(
            principal, personaReadModel, cancellationToken);
        if (!listenerResult.IsSuccess)
            return Result.Failure(listenerResult.Error!);

        var playlist = await playlistLoader.GetForMutationAsync(
            PlaylistId.From(playlistId), cancellationToken);
        if (playlist is null)
            return Result.Failure(DiscoveryErrors.PlaylistNotFound);

        if (playlist.OwnerListenerProfileId != listenerResult.Value!.ListenerProfileId)
            return Result.Failure(DiscoveryErrors.PlaylistForbidden);

        var trackIds = playlist.Items.Select(i => i.TrackId.Value).ToArray();
        if (trackIds.Length == 0)
            return Result.Failure(DiscoveryErrors.PlaylistItemNotFound);

        var rows = await catalog.GetPlayableTrackRowsAsync(trackIds, cancellationToken);
        var itemIdsToRemove = playlist.Items
            .Where(i => rows.TryGetValue(i.TrackId.Value, out var row) && row.ReleaseId == releaseId)
            .Select(i => i.Id)
            .ToArray();

        if (itemIdsToRemove.Length == 0)
            return Result.Failure(DiscoveryErrors.PlaylistItemNotFound);

        var removeResult = playlist.RemoveTracks(itemIdsToRemove, clock.UtcNow);
        if (!removeResult.IsSuccess)
            return Result.Failure(removeResult.Error!);

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
