using System.Security.Claims;
using Amuse.Domain.Discovery;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Common.Time;
using Amuse.Modules.Discovery.Features.Shared;
using Amuse.Modules.Discovery.Persistence;
using Amuse.Modules.Identity.Contracts;

namespace Amuse.Modules.Discovery.Features.ReplacePlaylistShares;

internal sealed class ReplacePlaylistSharesHandler(
    DiscoveryDbContext db,
    IListenerPersonaReadModel personaReadModel,
    PlaylistLoader playlistLoader,
    IClock clock)
{
    public async Task<Result> HandleAsync(
        Guid playlistId,
        ReplacePlaylistSharesRequest request,
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

        if (playlist.OwnerListenerProfileId != listenerResult.Value!.ListenerProfileId)
            return Result.Failure(DiscoveryErrors.PlaylistForbidden);

        var replaceResult = playlist.ReplaceShares(request.Emails, clock.UtcNow);
        if (!replaceResult.IsSuccess)
            return Result.Failure(replaceResult.Error!);

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
