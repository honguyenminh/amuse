using System.Security.Claims;
using Amuse.Domain.Discovery;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Common.Time;
using Amuse.Modules.Discovery.Features.Shared;
using Amuse.Modules.Discovery.Persistence;
using Amuse.Modules.Identity.Contracts;

namespace Amuse.Modules.Discovery.Features.RemoveTrackFromPlaylist;

internal sealed class RemoveTrackFromPlaylistHandler(
    DiscoveryDbContext db,
    IListenerPersonaReadModel personaReadModel,
    IClock clock)
{
    public async Task<Result> HandleAsync(
        Guid playlistId,
        Guid itemId,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        if (playlistId == Guid.Empty)
            return Result.Failure(DiscoveryErrors.PlaylistNotFound);

        if (itemId == Guid.Empty)
            return Result.Failure(DiscoveryErrors.PlaylistItemNotFound);

        var listenerResult = await DiscoveryPrincipal.RequireListenerAsync(
            principal, personaReadModel, cancellationToken);
        if (!listenerResult.IsSuccess)
            return Result.Failure(listenerResult.Error!);

        var playlist = await DiscoveryPlaylistLoader.LoadForMutationAsync(
            db, PlaylistId.From(playlistId), cancellationToken);
        if (playlist is null)
            return Result.Failure(DiscoveryErrors.PlaylistNotFound);

        if (playlist.OwnerListenerProfileId != listenerResult.Value!.ListenerProfileId)
            return Result.Failure(DiscoveryErrors.PlaylistForbidden);

        var removeResult = playlist.RemoveTrack(PlaylistItemId.From(itemId), clock.UtcNow);
        if (!removeResult.IsSuccess)
            return Result.Failure(removeResult.Error!);

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
