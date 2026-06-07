using Amuse.Domain.Discovery;
using Amuse.Domain.Listener;
using Amuse.Modules.Discovery.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Discovery.Features.Shared;

internal sealed class LikedPlaylistLoader(DiscoveryDbContext db)
{
    public async Task<Playlist?> GetForMutationAsync(
        ListenerProfileId listenerId,
        CancellationToken cancellationToken) =>
        await db.Playlists
            .Include(p => p.Items)
            .Include(p => p.ShareGrants)
            .FirstOrDefaultAsync(
                p => p.OwnerListenerProfileId == listenerId && p.Kind == PlaylistKind.Liked,
                cancellationToken);

    public async Task<Playlist> GetOrCreateForMutationAsync(
        ListenerProfileId listenerId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var existing = await GetForMutationAsync(listenerId, cancellationToken);
        if (existing is not null)
            return existing;

        var created = Playlist.CreateLiked(listenerId, now);
        if (!created.IsSuccess)
            throw new InvalidOperationException(created.Error!.Message);

        db.Playlists.Add(created.Value!);
        await db.SaveChangesAsync(cancellationToken);
        return created.Value!;
    }

    public async Task<Playlist?> GetForReadAsync(
        ListenerProfileId listenerId,
        CancellationToken cancellationToken) =>
        await db.Playlists.AsNoTracking()
            .Include(p => p.Items)
            .Include(p => p.ShareGrants)
            .FirstOrDefaultAsync(
                p => p.OwnerListenerProfileId == listenerId && p.Kind == PlaylistKind.Liked,
                cancellationToken);
}
