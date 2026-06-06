using Amuse.Domain.Discovery;
using Amuse.Domain.Listener;
using Amuse.Modules.Discovery.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Discovery.Features.Shared;

internal sealed record PlaylistEngagementState(
    HashSet<Guid> SavedPlaylistIds,
    HashSet<Guid> FollowedPlaylistIds);

internal static class DiscoveryEngagementQueries
{
    public static async Task<PlaylistEngagementState> GetPlaylistEngagementAsync(
        DiscoveryDbContext db,
        ListenerProfileId? listenerProfileId,
        IEnumerable<Guid> playlistIds,
        CancellationToken cancellationToken)
    {
        var ids = playlistIds.Distinct().ToArray();
        if (listenerProfileId is null || ids.Length == 0)
            return new PlaylistEngagementState([], []);

        var typedPlaylistIds = ids.Select(PlaylistId.From).ToArray();

        var saved = await db.LibraryEntries.AsNoTracking()
            .Where(e =>
                e.ListenerProfileId == listenerProfileId
                && e.Kind == LibraryEntryKind.SavedPlaylist
                && ids.Contains(e.TargetId))
            .Select(e => e.TargetId)
            .ToListAsync(cancellationToken);

        var followedRows = await db.PlaylistFollows.AsNoTracking()
            .Where(f =>
                f.ListenerProfileId == listenerProfileId
                && typedPlaylistIds.Contains(f.PlaylistId))
            .Select(f => f.PlaylistId)
            .ToListAsync(cancellationToken);

        return new PlaylistEngagementState(
            saved.ToHashSet(),
            followedRows.Select(id => id.Value).ToHashSet());
    }
}
