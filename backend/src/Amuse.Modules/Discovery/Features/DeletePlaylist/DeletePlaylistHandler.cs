using System.Security.Claims;
using Amuse.Domain.Discovery;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Discovery.Features.Shared;
using Amuse.Modules.Discovery.Persistence;
using Amuse.Modules.Identity.Contracts;

namespace Amuse.Modules.Discovery.Features.DeletePlaylist;

internal sealed class DeletePlaylistHandler(
    DiscoveryDbContext db,
    IListenerPersonaReadModel personaReadModel,
    PlaylistLoader playlistLoader)
{
    public async Task<Result> HandleAsync(
        Guid playlistId,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        if (playlistId == Guid.Empty)
            return Result.Failure(DiscoveryErrors.PlaylistNotFound);

        var listenerResult = await DiscoveryPrincipal.RequireListenerAsync(
            principal, personaReadModel, cancellationToken);
        if (!listenerResult.IsSuccess)
            return Result.Failure(listenerResult.Error!);

        var playlist = await playlistLoader.GetForMutationAsync(
            PlaylistId.From(playlistId), cancellationToken);
        if (playlist is null)
            return Result.Failure(DiscoveryErrors.PlaylistNotFound);

        var ownershipResult = playlist.EnsureOwnedBy(listenerResult.Value!.ListenerProfileId);
        if (!ownershipResult.IsSuccess)
            return Result.Failure(ownershipResult.Error!);

        var deletableResult = playlist.EnsureDeletable();
        if (!deletableResult.IsSuccess)
            return Result.Failure(deletableResult.Error!);

        db.Playlists.Remove(playlist);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
