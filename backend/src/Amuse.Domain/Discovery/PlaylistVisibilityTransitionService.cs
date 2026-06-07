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

    public static IReadOnlyList<Playlist> ApplyPrivateTransition(
        Playlist playlist,
        IEnumerable<Playlist> candidateForks,
        DateTimeOffset now)
    {
        var forks = GetForkDescendantsToCut(playlist, candidateForks);
        CutForkOrigins(forks, now);
        return forks;
    }
}
