using Amuse.Domain.Discovery;
using Amuse.Modules.Discovery.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Discovery.Features.Shared;

internal static class DiscoveryPlaylistLoader
{
    public static async Task<Playlist?> LoadForMutationAsync(
        DiscoveryDbContext db,
        PlaylistId playlistId,
        CancellationToken cancellationToken) =>
        await db.Playlists
            .Include(p => p.Items)
            .Include(p => p.ShareGrants)
            .FirstOrDefaultAsync(p => p.Id == playlistId, cancellationToken);

    public static async Task<Playlist?> LoadForReadAsync(
        DiscoveryDbContext db,
        PlaylistId playlistId,
        CancellationToken cancellationToken) =>
        await db.Playlists.AsNoTracking()
            .Include(p => p.Items)
            .Include(p => p.ShareGrants)
            .FirstOrDefaultAsync(p => p.Id == playlistId, cancellationToken);
}
