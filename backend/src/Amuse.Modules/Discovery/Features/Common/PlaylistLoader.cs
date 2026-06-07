using Amuse.Domain.Discovery;
using Amuse.Modules.Discovery.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Discovery.Features.Common;

internal sealed class PlaylistLoader(DiscoveryDbContext db)
{
    public async Task<Playlist?> GetForMutationAsync(
        PlaylistId playlistId,
        CancellationToken cancellationToken) =>
        await db.Playlists
            .Include(p => p.Items)
            .Include(p => p.ShareGrants)
            .FirstOrDefaultAsync(p => p.Id == playlistId, cancellationToken);

    public async Task<Playlist?> GetForAuthorizationAsync(
        PlaylistId playlistId,
        CancellationToken cancellationToken) =>
        await db.Playlists.AsNoTracking()
            .Include(p => p.ShareGrants)
            .FirstOrDefaultAsync(p => p.Id == playlistId, cancellationToken);

    public async Task<Playlist?> GetForReadAsync(
        PlaylistId playlistId,
        CancellationToken cancellationToken) =>
        await db.Playlists.AsNoTracking()
            .Include(p => p.Items)
            .Include(p => p.ShareGrants)
            .FirstOrDefaultAsync(p => p.Id == playlistId, cancellationToken);
}
