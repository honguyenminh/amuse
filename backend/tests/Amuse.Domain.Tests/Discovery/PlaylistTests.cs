using Amuse.Domain.Catalog;
using Amuse.Domain.Discovery;
using Amuse.Domain.Listener;

namespace Amuse.Domain.Tests.Discovery;

public sealed class PlaylistTests
{
    private static readonly ListenerProfileId Owner = ListenerProfileId.New();
    private static readonly ListenerProfileId Other = ListenerProfileId.New();
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-06-06T12:00:00+00:00");
    private static readonly TrackId Track1 = TrackId.New();
    private static readonly TrackId Track2 = TrackId.New();

    [Fact]
    public void CreateOwned_sets_title_and_visibility()
    {
        var result = Playlist.CreateOwned(Owner, "My Playlist", PlaylistVisibility.Private, Now);

        Assert.True(result.IsSuccess);
        Assert.Equal("My Playlist", result.Value!.Title);
        Assert.Equal(PlaylistVisibility.Private, result.Value.Visibility);
        Assert.Equal(Owner, result.Value.OwnerListenerProfileId);
    }

    [Fact]
    public void CreateOwned_rejects_empty_title()
    {
        var result = Playlist.CreateOwned(Owner, "   ", PlaylistVisibility.Private, Now);

        Assert.False(result.IsSuccess);
        Assert.Equal(DiscoveryErrors.InvalidPlaylistTitle.Code, result.Error!.Code);
    }

    [Fact]
    public void GrantShare_only_allowed_on_private_playlist()
    {
        var playlist = Playlist.CreateOwned(Owner, "Public List", PlaylistVisibility.Public, Now).Value!;
        var result = playlist.GrantShare("friend@example.com", Now);

        Assert.False(result.IsSuccess);
        Assert.Equal(DiscoveryErrors.ShareOnlyOnPrivatePlaylist.Code, result.Error!.Code);
    }

    [Fact]
    public void CanBeViewedBy_allows_owner_and_shared_email_on_private()
    {
        var playlist = Playlist.CreateOwned(Owner, "Private", PlaylistVisibility.Private, Now).Value!;
        Assert.True(playlist.GrantShare("friend@example.com", Now).IsSuccess);

        Assert.True(playlist.CanBeViewedBy(new PlaylistViewContext(Owner, null)));
        Assert.True(playlist.CanBeViewedBy(new PlaylistViewContext(null, "friend@example.com")));
        Assert.False(playlist.CanBeViewedBy(new PlaylistViewContext(Other, "stranger@example.com")));
    }

    [Fact]
    public void CanBeViewedBy_allows_anyone_on_public()
    {
        var playlist = Playlist.CreateOwned(Owner, "Public", PlaylistVisibility.Public, Now).Value!;

        Assert.True(playlist.CanBeViewedBy(new PlaylistViewContext(null, null)));
    }

    [Fact]
    public void AddTrack_rejects_duplicate()
    {
        var playlist = Playlist.CreateOwned(Owner, "List", PlaylistVisibility.Private, Now).Value!;
        Assert.True(playlist.AddTrack(Track1, Now).IsSuccess);

        var duplicate = playlist.AddTrack(Track1, Now);

        Assert.False(duplicate.IsSuccess);
        Assert.Equal(DiscoveryErrors.PlaylistTrackDuplicate.Code, duplicate.Error!.Code);
    }

    [Fact]
    public void Reorder_maintains_contiguous_positions()
    {
        var playlist = Playlist.CreateOwned(Owner, "List", PlaylistVisibility.Private, Now).Value!;
        var first = playlist.AddTrack(Track1, Now).Value!;
        var second = playlist.AddTrack(Track2, Now).Value!;

        Assert.True(playlist.Reorder(second.Id, 1, Now).IsSuccess);

        Assert.Equal(Track2, playlist.Items[0].TrackId);
        Assert.Equal(1, playlist.Items[0].Position);
        Assert.Equal(Track1, playlist.Items[1].TrackId);
        Assert.Equal(2, playlist.Items[1].Position);
    }

    [Fact]
    public void SetVisibility_public_to_private_sets_BecamePrivate_flag()
    {
        var playlist = Playlist.CreateOwned(Owner, "List", PlaylistVisibility.Public, Now).Value!;

        Assert.True(playlist.SetVisibility(PlaylistVisibility.Private, Now).IsSuccess);
        Assert.True(playlist.BecamePrivate);
    }

    [Fact]
    public void ForkFor_copies_tracks_and_sets_origin()
    {
        var source = Playlist.CreateOwned(Owner, "Source", PlaylistVisibility.Public, Now).Value!;
        Assert.True(source.AddTrack(Track1, Now).IsSuccess);
        Assert.True(source.AddTrack(Track2, Now).IsSuccess);

        var forkResult = source.ForkFor(Other, new PlaylistViewContext(Other, null), Now);

        Assert.True(forkResult.IsSuccess);
        var fork = forkResult.Value!;
        Assert.Equal(Other, fork.OwnerListenerProfileId);
        Assert.Equal(source.Id, fork.ForkedFromPlaylistId);
        Assert.Equal(2, fork.Items.Count);
        Assert.Equal(PlaylistVisibility.Private, fork.Visibility);
    }

    [Fact]
    public void ForkFor_denies_inaccessible_private_playlist()
    {
        var source = Playlist.CreateOwned(Owner, "Private", PlaylistVisibility.Private, Now).Value!;

        var forkResult = source.ForkFor(Other, new PlaylistViewContext(Other, null), Now);

        Assert.False(forkResult.IsSuccess);
        Assert.Equal(DiscoveryErrors.CannotForkPrivatePlaylist.Code, forkResult.Error!.Code);
    }

    [Fact]
    public void CutForkOrigin_clears_fork_link()
    {
        var sourceId = PlaylistId.New();
        var playlist = Playlist.Rehydrate(
            PlaylistId.New(),
            Other,
            "Fork",
            null,
            PlaylistKind.User,
            PlaylistVisibility.Private,
            sourceId,
            Now,
            Now,
            [],
            []);

        playlist.CutForkOrigin(Now);

        Assert.Null(playlist.ForkedFromPlaylistId);
    }

    [Fact]
    public void PlaylistFollow_rejects_own_playlist_and_private_playlist()
    {
        var own = Playlist.CreateOwned(Owner, "Mine", PlaylistVisibility.Public, Now).Value!;
        var ownFollow = PlaylistFollow.Create(Owner, own.Id, own, Now);
        Assert.False(ownFollow.IsSuccess);

        var priv = Playlist.CreateOwned(Other, "Private", PlaylistVisibility.Private, Now).Value!;
        var privateFollow = PlaylistFollow.Create(Owner, priv.Id, priv, Now);
        Assert.False(privateFollow.IsSuccess);
        Assert.Equal(DiscoveryErrors.FollowOnlyPublicPlaylist.Code, privateFollow.Error!.Code);
    }
}
