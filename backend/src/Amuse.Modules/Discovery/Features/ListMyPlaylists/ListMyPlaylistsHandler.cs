using System.Security.Claims;
using Amuse.Domain.Discovery;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Catalog.Contracts;
using Amuse.Modules.Discovery.Features.Shared;
using Amuse.Modules.Discovery.Persistence;
using Amuse.Modules.Identity.Contracts;
using Amuse.Modules.Listener.Contracts;
using Amuse.Modules.Media;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Discovery.Features.ListMyPlaylists;

internal sealed class ListMyPlaylistsHandler(
    DiscoveryDbContext db,
    ICatalogDiscoveryReadModel catalog,
    IListenerPersonaReadModel personaReadModel,
    IListenerProfilePresentationReadModel presentationReadModel,
    IMediaPublicUrlBuilder mediaUrls)
{
    public async Task<Result<PlaylistListResponse>> HandleAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var listenerResult = await DiscoveryPrincipal.RequireListenerAsync(
            principal, personaReadModel, cancellationToken);
        if (!listenerResult.IsSuccess)
            return Result<PlaylistListResponse>.Failure(listenerResult.Error!);

        var listenerId = listenerResult.Value!.ListenerProfileId;
        var playlists = await db.Playlists.AsNoTracking()
            .Include(p => p.Items)
            .Where(p => p.OwnerListenerProfileId == listenerId && p.Kind == PlaylistKind.User)
            .OrderByDescending(p => p.UpdatedAt)
            .ToListAsync(cancellationToken);

        var owners = await DiscoveryMapper.LoadOwnersAsync(
            playlists.Select(p => p.OwnerListenerProfileId),
            presentationReadModel,
            mediaUrls,
            cancellationToken);

        var coverArtUrls = await DiscoveryPlaylistCoverArt.LoadAsync(
            playlists,
            catalog,
            mediaUrls,
            cancellationToken);

        var summaries = playlists
            .Select(p =>
            {
                owners.TryGetValue(p.OwnerListenerProfileId.Value, out var owner);
                coverArtUrls.TryGetValue(p.Id.Value, out var covers);
                return DiscoveryMapper.ToSummary(p, owner, listenerId, null, covers);
            })
            .ToArray();

        return Result<PlaylistListResponse>.Success(new PlaylistListResponse(summaries));
    }
}
