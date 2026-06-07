using System.Security.Claims;
using Amuse.Domain.Discovery;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Discovery.Features.Shared;
using Amuse.Modules.Discovery.Persistence;
using Amuse.Modules.Identity.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Discovery.Features.UnsavePlaylist;

internal sealed class UnsavePlaylistHandler(
    DiscoveryDbContext db,
    IListenerPersonaReadModel personaReadModel)
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

        var listenerId = listenerResult.Value!.ListenerProfileId;
        var entries = await db.LibraryEntries
            .Where(e => e.ListenerProfileId == listenerId)
            .ToListAsync(cancellationToken);
        var library = ListenerLibrary.Rehydrate(listenerId, entries);

        var unsaveResult = library.TryUnsavePlaylist(PlaylistId.From(playlistId));
        if (!unsaveResult.IsSuccess)
            return Result.Failure(unsaveResult.Error!);

        if (unsaveResult.Value!.Count > 0)
            db.LibraryEntries.RemoveRange(unsaveResult.Value);

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
