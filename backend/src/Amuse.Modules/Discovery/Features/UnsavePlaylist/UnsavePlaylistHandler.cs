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

        var entry = await db.LibraryEntries.FirstOrDefaultAsync(
            e => e.ListenerProfileId == listenerResult.Value!.ListenerProfileId
                 && e.Kind == LibraryEntryKind.SavedPlaylist
                 && e.TargetId == playlistId,
            cancellationToken);

        if (entry is null)
            return Result.Success();

        db.LibraryEntries.Remove(entry);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
