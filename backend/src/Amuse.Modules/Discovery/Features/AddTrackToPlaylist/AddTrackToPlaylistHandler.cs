using System.Security.Claims;
using Amuse.Domain.Catalog;
using Amuse.Domain.Discovery;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Catalog.Contracts;
using Amuse.Modules.Common.Time;
using Amuse.Modules.Discovery.Features.Common;
using Amuse.Modules.Discovery.Persistence;
using Amuse.Modules.Identity.Contracts;
using Amuse.Modules.Media;

namespace Amuse.Modules.Discovery.Features.AddTrackToPlaylist;

internal sealed class AddTrackToPlaylistHandler(
    DiscoveryDbContext db,
    ICatalogDiscoveryReadModel catalog,
    IListenerPersonaReadModel personaReadModel,
    IMediaPublicUrlBuilder mediaUrls,
    PlaylistLoader playlistLoader,
    IClock clock)
{
    public async Task<Result<AddPlaylistItemResponse>> HandleAsync(
        Guid playlistId,
        AddPlaylistItemRequest request,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        if (playlistId == Guid.Empty)
            return Result<AddPlaylistItemResponse>.Failure(DiscoveryErrors.PlaylistNotFound);

        if (request.TrackId == Guid.Empty)
            return Result<AddPlaylistItemResponse>.Failure(DiscoveryErrors.InvalidTrackId);

        var listenerResult = await DiscoveryPrincipal.RequireListenerAsync(
            principal, personaReadModel, cancellationToken);
        if (!listenerResult.IsSuccess)
            return Result<AddPlaylistItemResponse>.Failure(listenerResult.Error!);

        var playlist = await playlistLoader.GetForMutationAsync(
            PlaylistId.From(playlistId), cancellationToken);
        if (playlist is null)
            return Result<AddPlaylistItemResponse>.Failure(DiscoveryErrors.PlaylistNotFound);

        if (playlist.OwnerListenerProfileId != listenerResult.Value!.ListenerProfileId)
            return Result<AddPlaylistItemResponse>.Failure(DiscoveryErrors.PlaylistForbidden);

        var trackId = TrackId.From(request.TrackId);
        if (!await catalog.TrackExistsAndPlayableAsync(trackId, cancellationToken))
            return Result<AddPlaylistItemResponse>.Failure(DiscoveryErrors.InvalidTrackId);

        var addResult = playlist.AddTrack(trackId, clock.UtcNow);
        if (!addResult.IsSuccess)
            return Result<AddPlaylistItemResponse>.Failure(addResult.Error!);

        await db.SaveChangesAsync(cancellationToken);

        var rows = await catalog.GetPlayableTrackRowsAsync([request.TrackId], cancellationToken);
        rows.TryGetValue(request.TrackId, out var row);
        var releaseSummaries = await catalog.GetReleaseSummariesAsync(
            row is not null ? [row.ReleaseId] : [],
            cancellationToken);
        releaseSummaries.TryGetValue(row?.ReleaseId ?? Guid.Empty, out var release);

        var itemDto = row is not null
            ? DiscoveryMapper.ToItemDto(addResult.Value!, row, mediaUrls, release?.CoverArtKey)
            : new PlaylistItemDto(
                addResult.Value!.Id.Value,
                request.TrackId,
                addResult.Value.Position,
                string.Empty,
                0,
                false,
                null,
                Guid.Empty,
                string.Empty,
                string.Empty);

        return Result<AddPlaylistItemResponse>.Success(new AddPlaylistItemResponse(itemDto));
    }
}
