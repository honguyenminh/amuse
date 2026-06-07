using System.Security.Claims;
using Amuse.Domain.Catalog;
using Amuse.Domain.Discovery;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Catalog.Contracts;
using Amuse.Modules.Common.Time;
using Amuse.Modules.Discovery.Features.Common;
using Amuse.Modules.Discovery.Persistence;
using Amuse.Modules.Identity.Contracts;

namespace Amuse.Modules.Discovery.Features.LikeTrack;

internal sealed class LikeTrackHandler(
    DiscoveryDbContext db,
    ICatalogDiscoveryReadModel catalog,
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

        var typedTrackId = TrackId.From(trackId);
        if (!await catalog.TrackExistsAndPlayableAsync(typedTrackId, cancellationToken))
            return Result.Failure(DiscoveryErrors.InvalidTrackId);

        var now = clock.UtcNow;
        var playlist = await likedPlaylistLoader.GetOrCreateForMutationAsync(
            listenerResult.Value!.ListenerProfileId,
            now,
            cancellationToken);

        var addResult = playlist.AddTrack(typedTrackId, now);
        if (!addResult.IsSuccess)
        {
            if (addResult.Error == DiscoveryErrors.PlaylistTrackDuplicate)
                return Result.Success();

            return Result.Failure(addResult.Error!);
        }

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
