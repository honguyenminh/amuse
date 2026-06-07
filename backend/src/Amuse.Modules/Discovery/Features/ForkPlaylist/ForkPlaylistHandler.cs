using System.Security.Claims;
using Amuse.Domain.Discovery;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Common.Time;
using Amuse.Modules.Discovery.Features.Shared;
using Amuse.Modules.Discovery.Persistence;
using Amuse.Modules.Identity.Contracts;
using Amuse.Modules.Listener.Contracts;
using Amuse.Modules.Media;

namespace Amuse.Modules.Discovery.Features.ForkPlaylist;

internal sealed class ForkPlaylistHandler(
    DiscoveryDbContext db,
    IListenerPersonaReadModel personaReadModel,
    IListenerProfilePresentationReadModel presentationReadModel,
    PlaylistViewContextBuilder viewContextBuilder,
    PlaylistLoader playlistLoader,
    IMediaPublicUrlBuilder mediaUrls,
    IClock clock)
{
    public async Task<Result<PlaylistDetailDto>> HandleAsync(
        Guid playlistId,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        if (playlistId == Guid.Empty)
            return Result<PlaylistDetailDto>.Failure(DiscoveryErrors.PlaylistNotFound);

        var listenerResult = await DiscoveryPrincipal.RequireListenerAsync(
            principal, personaReadModel, cancellationToken);
        if (!listenerResult.IsSuccess)
            return Result<PlaylistDetailDto>.Failure(listenerResult.Error!);

        var source = await playlistLoader.GetForReadAsync(
            PlaylistId.From(playlistId), cancellationToken);
        if (source is null)
            return Result<PlaylistDetailDto>.Failure(DiscoveryErrors.PlaylistNotFound);

        var viewContext = await viewContextBuilder.BuildForListenerAsync(
            listenerResult.Value!.ListenerProfileId,
            listenerResult.Value.AccountId,
            cancellationToken);

        var now = clock.UtcNow;
        var forkResult = source.ForkFor(listenerResult.Value.ListenerProfileId, viewContext, now);
        if (!forkResult.IsSuccess)
            return Result<PlaylistDetailDto>.Failure(forkResult.Error!);

        var fork = forkResult.Value!;
        db.Playlists.Add(fork);
        await db.SaveChangesAsync(cancellationToken);

        var owners = await DiscoveryMapper.LoadOwnersAsync(
            [fork.OwnerListenerProfileId],
            presentationReadModel,
            mediaUrls,
            cancellationToken);
        owners.TryGetValue(fork.OwnerListenerProfileId.Value, out var owner);

        return Result<PlaylistDetailDto>.Success(
            DiscoveryMapper.ToDetail(
                fork,
                [],
                owner,
                listenerResult.Value.ListenerProfileId,
                null,
                includeShareEmails: true));
    }
}
