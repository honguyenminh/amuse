using System.Security.Claims;
using Amuse.Domain.Discovery;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Common.Time;
using Amuse.Modules.Discovery.Features.Shared;
using Amuse.Modules.Discovery.Persistence;
using Amuse.Modules.Identity.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Discovery.Features.FollowPlaylist;

internal sealed class FollowPlaylistHandler(
    DiscoveryDbContext db,
    IListenerPersonaReadModel personaReadModel,
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

        var followResult = PlaylistFollow.Create(
            listenerResult.Value!.ListenerProfileId,
            PlaylistId.From(playlistId),
            playlist,
            clock.UtcNow);
        if (!followResult.IsSuccess)
            return Result.Failure(followResult.Error!);

        var exists = await db.PlaylistFollows.AnyAsync(
            f => f.ListenerProfileId == listenerResult.Value.ListenerProfileId
                 && f.PlaylistId == PlaylistId.From(playlistId),
            cancellationToken);
        if (exists)
            return Result.Success();

        db.PlaylistFollows.Add(followResult.Value!);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
