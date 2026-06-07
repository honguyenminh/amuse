using System.Security.Claims;
using Amuse.Domain.Catalog;
using Amuse.Domain.Discovery;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Common.Time;
using Amuse.Modules.Discovery.Features.Common;
using Amuse.Modules.Discovery.Persistence;
using Amuse.Modules.Identity.Contracts;

namespace Amuse.Modules.Discovery.Features.UnlikeTrack;

internal sealed class UnlikeTrackHandler(
    DiscoveryDbContext db,
    IListenerPersonaReadModel personaReadModel,
    LikedPlaylistLoader likedPlaylistLoader,
    IClock clock)
{
    public async Task<Result> HandleAsync(
        Guid trackId,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        if (trackId == Guid.Empty)
            return Result.Failure(DiscoveryErrors.InvalidTrackId);

        var listenerResult = await DiscoveryPrincipal.RequireListenerAsync(
            principal, personaReadModel, cancellationToken);
        if (!listenerResult.IsSuccess)
            return Result.Failure(listenerResult.Error!);

        var playlist = await likedPlaylistLoader.GetForMutationAsync(
            listenerResult.Value!.ListenerProfileId,
            cancellationToken);
        if (playlist is null)
            return Result.Failure(DiscoveryErrors.LikedTrackNotFound);

        var removeResult = playlist.RemoveTrackByTrackId(TrackId.From(trackId), clock.UtcNow);
        if (!removeResult.IsSuccess)
            return Result.Failure(removeResult.Error!);

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
