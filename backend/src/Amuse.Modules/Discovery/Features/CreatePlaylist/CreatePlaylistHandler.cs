using System.Security.Claims;
using Amuse.Domain.Discovery;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Common.Time;
using Amuse.Modules.Discovery.Features.Shared;
using Amuse.Modules.Discovery.Persistence;
using Amuse.Modules.Identity.Contracts;
using Amuse.Modules.Listener.Contracts;
using Amuse.Modules.Media;

namespace Amuse.Modules.Discovery.Features.CreatePlaylist;

internal sealed class CreatePlaylistHandler(
    DiscoveryDbContext db,
    IListenerPersonaReadModel personaReadModel,
    IListenerProfilePresentationReadModel presentationReadModel,
    IMediaPublicUrlBuilder mediaUrls,
    IClock clock)
{
    public async Task<Result<PlaylistDetailDto>> HandleAsync(
        CreatePlaylistRequest request,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var listenerResult = await DiscoveryPrincipal.RequireListenerAsync(
            principal, personaReadModel, cancellationToken);
        if (!listenerResult.IsSuccess)
            return Result<PlaylistDetailDto>.Failure(listenerResult.Error!);

        var visibilityResult = DiscoveryVisibility.TryParse(request.Visibility);
        if (!visibilityResult.IsSuccess)
            return Result<PlaylistDetailDto>.Failure(visibilityResult.Error!);

        var now = clock.UtcNow;
        var createResult = Playlist.CreateOwned(
            listenerResult.Value!.ListenerProfileId,
            request.Title,
            visibilityResult.Value,
            now);
        if (!createResult.IsSuccess)
            return Result<PlaylistDetailDto>.Failure(createResult.Error!);

        var playlist = createResult.Value!;
        if (request.Description is not null)
        {
            var descriptionResult = playlist.SetDescription(request.Description, now);
            if (!descriptionResult.IsSuccess)
                return Result<PlaylistDetailDto>.Failure(descriptionResult.Error!);
        }

        db.Playlists.Add(playlist);
        await db.SaveChangesAsync(cancellationToken);

        var owners = await DiscoveryMapper.LoadOwnersAsync(
            [playlist.OwnerListenerProfileId],
            presentationReadModel,
            mediaUrls,
            cancellationToken);
        owners.TryGetValue(playlist.OwnerListenerProfileId.Value, out var owner);

        return Result<PlaylistDetailDto>.Success(
            DiscoveryMapper.ToDetail(
                playlist,
                [],
                owner,
                listenerResult.Value.ListenerProfileId,
                null,
                includeShareEmails: true));
    }
}
