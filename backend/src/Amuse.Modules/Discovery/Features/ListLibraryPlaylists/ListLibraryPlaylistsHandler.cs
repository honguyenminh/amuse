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

namespace Amuse.Modules.Discovery.Features.ListLibraryPlaylists;

internal sealed class ListLibraryPlaylistsHandler(
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

        var owned = await db.Playlists.AsNoTracking()
            .Include(p => p.Items)
            .Where(p => p.OwnerListenerProfileId == listenerId)
            .ToListAsync(cancellationToken);

        var savedIds = await db.LibraryEntries.AsNoTracking()
            .Where(e =>
                e.ListenerProfileId == listenerId
                && e.Kind == LibraryEntryKind.SavedPlaylist)
            .Select(e => e.TargetId)
            .ToListAsync(cancellationToken);

        var ownedIds = owned.Select(p => p.Id.Value).ToHashSet();
        var savedOnlyIds = savedIds.Where(id => !ownedIds.Contains(id)).ToArray();

        var savedPlaylistIds = savedOnlyIds.Select(PlaylistId.From).ToArray();
        var savedPlaylists = savedPlaylistIds.Length > 0
            ? await db.Playlists.AsNoTracking()
                .Include(p => p.Items)
                .Where(p => savedPlaylistIds.Contains(p.Id))
                .ToListAsync(cancellationToken)
            : [];

        var followedIds = await db.PlaylistFollows.AsNoTracking()
            .Where(f => f.ListenerProfileId == listenerId)
            .Select(f => f.PlaylistId.Value)
            .ToListAsync(cancellationToken);

        var includedIds = ownedIds.Union(savedIds).ToHashSet();
        var followedOnlyIds = followedIds
            .Where(id => !includedIds.Contains(id))
            .ToArray();

        var followedPlaylistIds = followedOnlyIds.Select(PlaylistId.From).ToArray();
        var followedPlaylists = followedPlaylistIds.Length > 0
            ? await db.Playlists.AsNoTracking()
                .Include(p => p.Items)
                .Where(p => followedPlaylistIds.Contains(p.Id))
                .ToListAsync(cancellationToken)
            : [];

        var allPlaylists = owned.Concat(savedPlaylists).Concat(followedPlaylists)
            .OrderByDescending(p => p.UpdatedAt)
            .ToList();

        var engagement = await DiscoveryEngagementQueries.GetPlaylistEngagementAsync(
            db,
            listenerId,
            allPlaylists.Select(p => p.Id.Value),
            cancellationToken);

        var owners = await DiscoveryMapper.LoadOwnersAsync(
            allPlaylists.Select(p => p.OwnerListenerProfileId),
            presentationReadModel,
            mediaUrls,
            cancellationToken);

        var coverArtUrls = await DiscoveryPlaylistCoverArt.LoadAsync(
            allPlaylists,
            catalog,
            mediaUrls,
            cancellationToken);

        var summaries = allPlaylists
            .Select(p =>
            {
                owners.TryGetValue(p.OwnerListenerProfileId.Value, out var owner);
                coverArtUrls.TryGetValue(p.Id.Value, out var covers);
                return DiscoveryMapper.ToSummary(p, owner, listenerId, engagement, covers);
            })
            .ToArray();

        return Result<PlaylistListResponse>.Success(new PlaylistListResponse(summaries));
    }
}
