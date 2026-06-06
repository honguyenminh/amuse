using Amuse.Domain.SharedKernel;

namespace Amuse.Domain.Discovery;

public static class DiscoveryErrors
{
    public static readonly DomainError PlaylistNotFound =
        new("discovery.playlist_not_found", "Playlist was not found.");

    public static readonly DomainError PlaylistForbidden =
        new("discovery.playlist_forbidden", "You do not have access to this playlist.");

    public static readonly DomainError InvalidPlaylistTitle =
        new("discovery.invalid_playlist_title", "Playlist title is invalid.");

    public static readonly DomainError InvalidPlaylistDescription =
        new("discovery.invalid_playlist_description", "Playlist description is invalid.");

    public static readonly DomainError InvalidShareEmail =
        new("discovery.invalid_share_email", "Share email address is invalid.");

    public static readonly DomainError ShareOnlyOnPrivatePlaylist =
        new("discovery.share_only_on_private_playlist", "Share grants are only allowed on private playlists.");

    public static readonly DomainError PlaylistTrackDuplicate =
        new("discovery.playlist_track_duplicate", "Track is already in this playlist.");

    public static readonly DomainError PlaylistItemNotFound =
        new("discovery.playlist_item_not_found", "Playlist item was not found.");

    public static readonly DomainError InvalidPlaylistPosition =
        new("discovery.invalid_playlist_position", "Playlist position is invalid.");

    public static readonly DomainError CannotForkPrivatePlaylist =
        new("discovery.cannot_fork_private_playlist", "Cannot fork a playlist you cannot access.");

    public static readonly DomainError CannotFollowOwnPlaylist =
        new("discovery.cannot_follow_own_playlist", "You cannot follow your own playlist.");

    public static readonly DomainError FollowOnlyPublicPlaylist =
        new("discovery.follow_only_public_playlist", "Only public playlists can be followed.");

    public static readonly DomainError LibraryEntryExists =
        new("discovery.library_entry_exists", "This item is already in your library.");

    public static readonly DomainError LibraryEntryNotFound =
        new("discovery.library_entry_not_found", "Library entry was not found.");

    public static readonly DomainError LikedTrackNotFound =
        new("discovery.liked_track_not_found", "Liked track was not found.");

    public static readonly DomainError InvalidSearchQuery =
        new("discovery.search_query_invalid", "Search query is invalid.");

    public static readonly DomainError InvalidTrackId =
        new("discovery.invalid_track_id", "Track id is invalid.");

    public static readonly DomainError InvalidReleaseId =
        new("discovery.invalid_release_id", "Release id is invalid.");

    public static readonly DomainError CannotDeleteLikedPlaylist =
        new("discovery.cannot_delete_liked_playlist", "The liked collection cannot be deleted.");

    public static readonly DomainError CannotForkLikedPlaylist =
        new("discovery.cannot_fork_liked_playlist", "The liked collection cannot be forked.");

    public static readonly DomainError CannotRenameLikedPlaylist =
        new("discovery.cannot_rename_liked_playlist", "The liked collection title cannot be changed.");
}
