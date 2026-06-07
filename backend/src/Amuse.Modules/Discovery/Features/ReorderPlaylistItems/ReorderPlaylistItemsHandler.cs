using System.Security.Claims;
using Amuse.Domain.Discovery;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Common.Time;
using Amuse.Modules.Discovery.Features.Common;
using Amuse.Modules.Discovery.Persistence;
using Amuse.Modules.Identity.Contracts;

namespace Amuse.Modules.Discovery.Features.ReorderPlaylistItems;

internal sealed class ReorderPlaylistItemsHandler(
    DiscoveryDbContext db,
    IListenerPersonaReadModel personaReadModel,
    PlaylistLoader playlistLoader,
    IClock clock)
{
    public async Task<Result> HandleAsync(
        Guid playlistId,
        ReorderPlaylistItemsRequest request,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        if (playlistId == Guid.Empty)
            return Result.Failure(DiscoveryErrors.PlaylistNotFound);

        if (request.ItemId == Guid.Empty)
            return Result.Failure(DiscoveryErrors.PlaylistItemNotFound);

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

        var reorderResult = playlist.Reorder(
            PlaylistItemId.From(request.ItemId),
            request.NewPosition,
            clock.UtcNow);
        if (!reorderResult.IsSuccess)
            return Result.Failure(reorderResult.Error!);

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
