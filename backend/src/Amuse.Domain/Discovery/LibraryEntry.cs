using Amuse.Domain.Listener;

namespace Amuse.Domain.Discovery;

public sealed class LibraryEntry
{
    public LibraryEntryId Id { get; private set; }
    public ListenerProfileId ListenerProfileId { get; private set; }
    public LibraryEntryKind Kind { get; private set; }
    public Guid TargetId { get; private set; }
    public DateTimeOffset SavedAt { get; private set; }

    private LibraryEntry()
    {
    }

    public static LibraryEntry CreateSavedPlaylist(
        ListenerProfileId listenerId,
        PlaylistId playlistId,
        DateTimeOffset now) =>
        Create(listenerId, LibraryEntryKind.SavedPlaylist, playlistId.Value, now);

    public static LibraryEntry CreateSavedRelease(
        ListenerProfileId listenerId,
        Guid releaseId,
        DateTimeOffset now) =>
        Create(listenerId, LibraryEntryKind.SavedRelease, releaseId, now);

    public static LibraryEntry Rehydrate(
        LibraryEntryId id,
        ListenerProfileId listenerId,
        LibraryEntryKind kind,
        Guid targetId,
        DateTimeOffset savedAt) =>
        new()
        {
            Id = id,
            ListenerProfileId = listenerId,
            Kind = kind,
            TargetId = targetId,
            SavedAt = savedAt,
        };

    private static LibraryEntry Create(
        ListenerProfileId listenerId,
        LibraryEntryKind kind,
        Guid targetId,
        DateTimeOffset now) =>
        new()
        {
            Id = LibraryEntryId.New(),
            ListenerProfileId = listenerId,
            Kind = kind,
            TargetId = targetId,
            SavedAt = now,
        };
}
