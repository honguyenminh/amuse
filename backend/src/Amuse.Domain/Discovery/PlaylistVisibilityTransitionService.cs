namespace Amuse.Domain.Discovery;

public static class PlaylistVisibilityTransitionService
{
    public static IReadOnlyList<Playlist> GetForkDescendantsToCut(
        Playlist playlist,
        IEnumerable<Playlist> candidateForks)
    {
        if (!playlist.BecamePrivate)
            return [];

        return candidateForks
            .Where(f => f.ForkedFromPlaylistId == playlist.Id)
            .ToList();
    }

    public static void CutForkOrigins(IEnumerable<Playlist> forks, DateTimeOffset now)
    {
        foreach (var fork in forks)
            fork.CutForkOrigin(now);
    }
}
