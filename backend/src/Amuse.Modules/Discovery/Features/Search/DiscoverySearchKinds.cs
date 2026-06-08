namespace Amuse.Modules.Discovery.Features.Search;

internal static class DiscoverySearchKinds
{
    internal const string Artist = "artist";
    internal const string Release = "release";
    internal const string Track = "track";
    internal const string Playlist = "playlist";

    private static readonly HashSet<string> All =
    [
        Artist,
        Release,
        Track,
        Playlist,
    ];

    internal static IReadOnlySet<string>? Parse(string[]? kinds)
    {
        if (kinds is null or { Length: 0 })
            return null;

        var parsed = kinds
            .SelectMany(kind => kind.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Select(kind => kind.ToLowerInvariant())
            .Where(All.Contains)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (parsed.Count == 0 || parsed.Count == All.Count)
            return null;

        return parsed;
    }

    internal static bool IncludesCatalog(IReadOnlySet<string>? kinds) =>
        kinds is null
        || kinds.Contains(Artist)
        || kinds.Contains(Release)
        || kinds.Contains(Track);

    internal static bool IncludesArtist(IReadOnlySet<string>? kinds) =>
        kinds is null || kinds.Contains(Artist);

    internal static bool IncludesRelease(IReadOnlySet<string>? kinds) =>
        kinds is null || kinds.Contains(Release);

    internal static bool IncludesTrack(IReadOnlySet<string>? kinds) =>
        kinds is null || kinds.Contains(Track);

    internal static bool IncludesPlaylist(IReadOnlySet<string>? kinds) =>
        kinds is null || kinds.Contains(Playlist);
}
