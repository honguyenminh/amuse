using Amuse.Domain.Discovery;
using Amuse.Domain.Listener;

namespace Amuse.Domain.Tests.Discovery;

public sealed class ListenerLibraryTests
{
    private static readonly ListenerProfileId Listener = ListenerProfileId.New();
    private static readonly ListenerProfileId Other = ListenerProfileId.New();
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-06-06T12:00:00+00:00");

    [Fact]
    public void TrySavePlaylist_adds_entry_when_viewable()
    {
        var playlist = Playlist.CreateOwned(Other, "Shared", PlaylistVisibility.Public, Now).Value!;
        var library = ListenerLibrary.Rehydrate(Listener, []);

        var result = library.TrySavePlaylist(playlist, new PlaylistViewContext(Listener, null), Now);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Single(library.Entries);
        Assert.Equal(LibraryEntryKind.SavedPlaylist, library.Entries[0].Kind);
        Assert.Equal(playlist.Id.Value, library.Entries[0].TargetId);
    }

    [Fact]
    public void TrySavePlaylist_rejects_inaccessible_playlist()
    {
        var playlist = Playlist.CreateOwned(Other, "Private", PlaylistVisibility.Private, Now).Value!;
        var library = ListenerLibrary.Rehydrate(Listener, []);

        var result = library.TrySavePlaylist(playlist, new PlaylistViewContext(Listener, null), Now);

        Assert.False(result.IsSuccess);
        Assert.Equal(DiscoveryErrors.PlaylistForbidden.Code, result.Error!.Code);
        Assert.Empty(library.Entries);
    }

    [Fact]
    public void TrySavePlaylist_is_idempotent_when_already_saved()
    {
        var playlist = Playlist.CreateOwned(Other, "Public", PlaylistVisibility.Public, Now).Value!;
        var existing = LibraryEntry.CreateSavedPlaylist(Listener, playlist.Id, Now);
        var library = ListenerLibrary.Rehydrate(Listener, [existing]);

        var result = library.TrySavePlaylist(playlist, new PlaylistViewContext(Listener, null), Now);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value);
        Assert.Single(library.Entries);
    }

    [Fact]
    public void TryUnsavePlaylist_is_idempotent_when_not_saved()
    {
        var playlistId = PlaylistId.New();
        var library = ListenerLibrary.Rehydrate(Listener, []);

        var result = library.TryUnsavePlaylist(playlistId);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!);
        Assert.Empty(library.Entries);
    }

    [Fact]
    public void TryUnsavePlaylist_removes_saved_entry()
    {
        var playlistId = PlaylistId.New();
        var entry = LibraryEntry.CreateSavedPlaylist(Listener, playlistId, Now);
        var library = ListenerLibrary.Rehydrate(Listener, [entry]);

        var result = library.TryUnsavePlaylist(playlistId);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!);
        Assert.Empty(library.Entries);
    }

    [Fact]
    public void TrySaveRelease_adds_entry()
    {
        var releaseId = Guid.NewGuid();
        var library = ListenerLibrary.Rehydrate(Listener, []);

        var result = library.TrySaveRelease(releaseId, Now);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Single(library.Entries);
        Assert.Equal(LibraryEntryKind.SavedRelease, library.Entries[0].Kind);
    }

    [Fact]
    public void TrySaveRelease_rejects_empty_release_id()
    {
        var library = ListenerLibrary.Rehydrate(Listener, []);

        var result = library.TrySaveRelease(Guid.Empty, Now);

        Assert.False(result.IsSuccess);
        Assert.Equal(DiscoveryErrors.InvalidReleaseId.Code, result.Error!.Code);
    }

    [Fact]
    public void TryUnsaveRelease_is_idempotent_when_not_saved()
    {
        var releaseId = Guid.NewGuid();
        var library = ListenerLibrary.Rehydrate(Listener, []);

        var result = library.TryUnsaveRelease(releaseId);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!);
    }

    [Fact]
    public void TryUnsaveRelease_removes_saved_entry()
    {
        var releaseId = Guid.NewGuid();
        var entry = LibraryEntry.CreateSavedRelease(Listener, releaseId, Now);
        var library = ListenerLibrary.Rehydrate(Listener, [entry]);

        var result = library.TryUnsaveRelease(releaseId);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!);
        Assert.Empty(library.Entries);
    }
}
