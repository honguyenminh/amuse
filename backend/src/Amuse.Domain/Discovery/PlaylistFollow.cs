using Amuse.Domain.Listener;
using Amuse.Domain.SharedKernel;

namespace Amuse.Domain.Discovery;

public sealed class PlaylistFollow
{
    public ListenerProfileId ListenerProfileId { get; private set; }
    public PlaylistId PlaylistId { get; private set; }
    public DateTimeOffset FollowedAt { get; private set; }

    private PlaylistFollow()
    {
    }

    public static Result<PlaylistFollow> Create(
        ListenerProfileId listenerId,
        PlaylistId playlistId,
        Playlist playlist,
        DateTimeOffset now)
    {
        if (playlist.OwnerListenerProfileId == listenerId)
            return Result<PlaylistFollow>.Failure(DiscoveryErrors.CannotFollowOwnPlaylist);

        if (playlist.Visibility != PlaylistVisibility.Public)
            return Result<PlaylistFollow>.Failure(DiscoveryErrors.FollowOnlyPublicPlaylist);

        return Result<PlaylistFollow>.Success(new PlaylistFollow
        {
            ListenerProfileId = listenerId,
            PlaylistId = playlistId,
            FollowedAt = now,
        });
    }

    public static PlaylistFollow Rehydrate(
        ListenerProfileId listenerId,
        PlaylistId playlistId,
        DateTimeOffset followedAt) =>
        new()
        {
            ListenerProfileId = listenerId,
            PlaylistId = playlistId,
            FollowedAt = followedAt,
        };
}
