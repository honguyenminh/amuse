using Amuse.Domain.Discovery;
using Amuse.Domain.Listener;
using Amuse.Modules.Discovery.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Discovery.Features.Shared;

internal static class LikedPlaylistService
{
    public static async Task<Playlist> GetOrCreateForMutationAsync(
        DiscoveryDbContext db,
        ListenerProfileId listenerId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var existing = await db.Playlists
            .Include(p => p.Items)
            .Include(p => p.ShareGrants)
            .FirstOrDefaultAsync(
                p => p.OwnerListenerProfileId == listenerId && p.Kind == PlaylistKind.Liked,
                cancellationToken);
        if (existing is not null)
            return existing;

        var created = Playlist.CreateLiked(listenerId, now);
        if (!created.IsSuccess)
            throw new InvalidOperationException(created.Error!.Message);

        db.Playlists.Add(created.Value!);
        await db.SaveChangesAsync(cancellationToken);
        return created.Value!;
    }

    public static async Task<Playlist?> LoadForReadAsync(
        DiscoveryDbContext db,
        ListenerProfileId listenerId,
        CancellationToken cancellationToken) =>
        await db.Playlists.AsNoTracking()
            .Include(p => p.Items)
            .Include(p => p.ShareGrants)
            .FirstOrDefaultAsync(
                p => p.OwnerListenerProfileId == listenerId && p.Kind == PlaylistKind.Liked,
                cancellationToken);
}
