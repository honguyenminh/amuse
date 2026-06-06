using System.Security.Claims;
using Amuse.Domain.Discovery;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Common.Time;
using Amuse.Modules.Discovery.Features.Shared;
using Amuse.Modules.Discovery.Persistence;
using Amuse.Modules.Identity.Contracts;
using Amuse.Modules.Listener.Contracts;
using Amuse.Modules.Media;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Discovery.Features.UpdatePlaylist;

internal sealed class UpdatePlaylistHandler(
    DiscoveryDbContext db,
    IListenerPersonaReadModel personaReadModel,
    IListenerProfilePresentationReadModel presentationReadModel,
    IObjectStorage storage,
    IClock clock)
{
    public async Task<Result<PlaylistDetailDto>> HandleAsync(
        Guid playlistId,
        UpdatePlaylistRequest request,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        if (playlistId == Guid.Empty)
            return Result<PlaylistDetailDto>.Failure(DiscoveryErrors.PlaylistNotFound);

        var listenerResult = await DiscoveryPrincipal.RequireListenerAsync(
            principal, personaReadModel, cancellationToken);
        if (!listenerResult.IsSuccess)
            return Result<PlaylistDetailDto>.Failure(listenerResult.Error!);

        var playlist = await DiscoveryPlaylistLoader.LoadForMutationAsync(
            db, PlaylistId.From(playlistId), cancellationToken);
        if (playlist is null)
            return Result<PlaylistDetailDto>.Failure(DiscoveryErrors.PlaylistNotFound);

        if (playlist.OwnerListenerProfileId != listenerResult.Value!.ListenerProfileId)
            return Result<PlaylistDetailDto>.Failure(DiscoveryErrors.PlaylistForbidden);

        var now = clock.UtcNow;

        if (request.Title is not null)
        {
            var renameResult = playlist.Rename(request.Title, now);
            if (!renameResult.IsSuccess)
                return Result<PlaylistDetailDto>.Failure(renameResult.Error!);
        }

        if (request.Description is not null || request.Description == string.Empty)
        {
            var descriptionResult = playlist.SetDescription(
                request.Description == string.Empty ? null : request.Description,
                now);
            if (!descriptionResult.IsSuccess)
                return Result<PlaylistDetailDto>.Failure(descriptionResult.Error!);
        }

        if (request.Visibility is not null)
        {
            var visibilityResult = DiscoveryVisibility.TryParse(request.Visibility);
            if (!visibilityResult.IsSuccess)
                return Result<PlaylistDetailDto>.Failure(visibilityResult.Error!);

            var setVisibilityResult = playlist.SetVisibility(visibilityResult.Value, now);
            if (!setVisibilityResult.IsSuccess)
                return Result<PlaylistDetailDto>.Failure(setVisibilityResult.Error!);

            if (playlist.BecamePrivate)
            {
                var forks = await db.Playlists
                    .Where(p => p.ForkedFromPlaylistId == playlist.Id)
                    .ToListAsync(cancellationToken);
                var toCut = PlaylistVisibilityTransitionService.GetForkDescendantsToCut(playlist, forks);
                PlaylistVisibilityTransitionService.CutForkOrigins(toCut, now);

                var follows = await db.PlaylistFollows
                    .Where(f => f.PlaylistId == playlist.Id)
                    .ToListAsync(cancellationToken);
                db.PlaylistFollows.RemoveRange(follows);
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        var owners = await DiscoveryMapper.LoadOwnersAsync(
            [playlist.OwnerListenerProfileId],
            presentationReadModel,
            storage,
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
