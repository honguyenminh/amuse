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
    PlaylistLoader playlistLoader,
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

        var typedPlaylistId = PlaylistId.From(playlistId);
        var playlist = await playlistLoader.GetForAuthorizationAsync(typedPlaylistId, cancellationToken);
        if (playlist is null)
            return Result.Failure(DiscoveryErrors.PlaylistNotFound);

        var viewContext = await viewContextBuilder.BuildForListenerAsync(
            listenerResult.Value!.ListenerProfileId,
            listenerResult.Value.AccountId,
            cancellationToken);

        var listenerId = listenerResult.Value.ListenerProfileId;
        var entries = await db.LibraryEntries
            .Where(e => e.ListenerProfileId == listenerId)
            .ToListAsync(cancellationToken);
        var library = ListenerLibrary.Rehydrate(listenerId, entries);

        var saveResult = library.TrySavePlaylist(playlist, viewContext, clock.UtcNow);
        if (!saveResult.IsSuccess)
            return Result.Failure(saveResult.Error!);

        if (saveResult.Value is not null)
            db.LibraryEntries.Add(saveResult.Value);

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
