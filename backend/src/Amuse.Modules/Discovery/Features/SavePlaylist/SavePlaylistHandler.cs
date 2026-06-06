using System.Security.Claims;
using Amuse.Domain.Discovery;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Common.Time;
using Amuse.Modules.Discovery.Features.Shared;
using Amuse.Modules.Discovery.Persistence;
using Amuse.Modules.Identity.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Discovery.Features.SavePlaylist;

internal sealed class SavePlaylistHandler(
    DiscoveryDbContext db,
    IListenerPersonaReadModel personaReadModel,
    PlaylistViewContextBuilder viewContextBuilder,
    IClock clock)
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

        var playlist = await db.Playlists.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == PlaylistId.From(playlistId), cancellationToken);
        if (playlist is null)
            return Result.Failure(DiscoveryErrors.PlaylistNotFound);

        var viewContext = await viewContextBuilder.BuildForListenerAsync(
            listenerResult.Value!.ListenerProfileId,
            listenerResult.Value.AccountId,
            cancellationToken);
        if (!playlist.CanBeViewedBy(viewContext))
            return Result.Failure(DiscoveryErrors.PlaylistForbidden);

        var exists = await db.LibraryEntries.AnyAsync(
            e => e.ListenerProfileId == listenerResult.Value.ListenerProfileId
                 && e.Kind == LibraryEntryKind.SavedPlaylist
                 && e.TargetId == playlistId,
            cancellationToken);
        if (exists)
            return Result.Success();

        var entry = LibraryEntry.CreateSavedPlaylist(
            listenerResult.Value.ListenerProfileId,
            PlaylistId.From(playlistId),
            clock.UtcNow);
        db.LibraryEntries.Add(entry);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
