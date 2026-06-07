using Amuse.Domain.Listener;
using Amuse.Domain.SharedKernel;

namespace Amuse.Domain.Discovery;

public sealed class ListenerLibrary
{
    public ListenerProfileId ListenerProfileId { get; private set; }

    private readonly List<LibraryEntry> _entries = [];

    public IReadOnlyList<LibraryEntry> Entries => _entries;

    private ListenerLibrary()
    {
    }

    public static ListenerLibrary Rehydrate(
        ListenerProfileId listenerId,
        IEnumerable<LibraryEntry> entries)
    {
        var library = new ListenerLibrary
        {
            ListenerProfileId = listenerId,
        };
        library._entries.AddRange(entries);
        return library;
    }

    public Result<LibraryEntry?> TrySavePlaylist(
        Playlist playlist,
        PlaylistViewContext viewContext,
        DateTimeOffset now)
    {
        if (!playlist.CanBeViewedBy(viewContext))
            return Result<LibraryEntry?>.Failure(DiscoveryErrors.PlaylistForbidden);

        if (HasSavedPlaylist(playlist.Id))
            return Result<LibraryEntry?>.Success(null);

        var entry = LibraryEntry.CreateSavedPlaylist(ListenerProfileId, playlist.Id, now);
        _entries.Add(entry);
        return Result<LibraryEntry?>.Success(entry);
    }

    public Result<IReadOnlyList<LibraryEntry>> TryUnsavePlaylist(PlaylistId playlistId)
    {
        var removed = RemoveEntries(LibraryEntryKind.SavedPlaylist, playlistId.Value);
        return Result<IReadOnlyList<LibraryEntry>>.Success(removed);
    }

    public Result<LibraryEntry?> TrySaveRelease(Guid releaseId, DateTimeOffset now)
    {
        if (releaseId == Guid.Empty)
            return Result<LibraryEntry?>.Failure(DiscoveryErrors.InvalidReleaseId);

        if (HasSavedRelease(releaseId))
            return Result<LibraryEntry?>.Success(null);

        var entry = LibraryEntry.CreateSavedRelease(ListenerProfileId, releaseId, now);
        _entries.Add(entry);
        return Result<LibraryEntry?>.Success(entry);
    }

    public Result<IReadOnlyList<LibraryEntry>> TryUnsaveRelease(Guid releaseId)
    {
        if (releaseId == Guid.Empty)
            return Result<IReadOnlyList<LibraryEntry>>.Failure(DiscoveryErrors.InvalidReleaseId);

        var removed = RemoveEntries(LibraryEntryKind.SavedRelease, releaseId);
        return Result<IReadOnlyList<LibraryEntry>>.Success(removed);
    }

    private bool HasSavedPlaylist(PlaylistId playlistId) =>
        _entries.Any(e => e.Kind == LibraryEntryKind.SavedPlaylist && e.TargetId == playlistId.Value);

    private bool HasSavedRelease(Guid releaseId) =>
        _entries.Any(e => e.Kind == LibraryEntryKind.SavedRelease && e.TargetId == releaseId);

    private List<LibraryEntry> RemoveEntries(LibraryEntryKind kind, Guid targetId)
    {
        var removed = _entries.Where(e => e.Kind == kind && e.TargetId == targetId).ToList();
        _entries.RemoveAll(e => e.Kind == kind && e.TargetId == targetId);
        return removed;
    }
}
