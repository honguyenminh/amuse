using System.Security.Claims;
using Amuse.Domain.Discovery;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Discovery.Features.Common;
using Amuse.Modules.Discovery.Persistence;
using Amuse.Modules.Identity.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Discovery.Features.UnfollowPlaylist;

internal sealed class UnfollowPlaylistHandler(
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

        var follow = await db.PlaylistFollows.FirstOrDefaultAsync(
            f => f.ListenerProfileId == listenerResult.Value!.ListenerProfileId
                 && f.PlaylistId == PlaylistId.From(playlistId),
            cancellationToken);

        if (follow is null)
            return Result.Success();

        db.PlaylistFollows.Remove(follow);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
