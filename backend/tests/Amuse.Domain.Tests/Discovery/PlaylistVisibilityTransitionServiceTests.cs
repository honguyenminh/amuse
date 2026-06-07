using Amuse.Domain.Discovery;
using Amuse.Domain.Listener;

namespace Amuse.Domain.Tests.Discovery;

public sealed class PlaylistVisibilityTransitionServiceTests
{
    private static readonly ListenerProfileId Owner = ListenerProfileId.New();
    private static readonly ListenerProfileId ForkOwner = ListenerProfileId.New();
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-06-06T12:00:00+00:00");

    [Fact]
    public void ApplyPrivateTransition_cuts_fork_origins_when_playlist_became_private()
    {
        var source = Playlist.CreateOwned(Owner, "Source", PlaylistVisibility.Public, Now).Value!;
        Assert.True(source.SetVisibility(PlaylistVisibility.Private, Now).IsSuccess);

        var fork = Playlist.Rehydrate(
            PlaylistId.New(),
            ForkOwner,
            "Fork",
            null,
            PlaylistKind.User,
            PlaylistVisibility.Private,
            source.Id,
            Now,
            Now,
            [],
            []);

        var affected = PlaylistVisibilityTransitionService.ApplyPrivateTransition(
            source,
            [fork],
            Now);

        Assert.Single(affected);
        Assert.Null(fork.ForkedFromPlaylistId);
    }

    [Fact]
    public void ApplyPrivateTransition_does_nothing_when_playlist_did_not_become_private()
    {
        var source = Playlist.CreateOwned(Owner, "Private", PlaylistVisibility.Private, Now).Value!;
        var fork = Playlist.Rehydrate(
            PlaylistId.New(),
            ForkOwner,
            "Fork",
            null,
            PlaylistKind.User,
            PlaylistVisibility.Private,
            source.Id,
            Now,
            Now,
            [],
            []);

        var affected = PlaylistVisibilityTransitionService.ApplyPrivateTransition(
            source,
            [fork],
            Now);

        Assert.Empty(affected);
        Assert.Equal(source.Id, fork.ForkedFromPlaylistId);
    }
}
