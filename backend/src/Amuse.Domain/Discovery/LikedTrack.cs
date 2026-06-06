using Amuse.Domain.Catalog;
using Amuse.Domain.Listener;

namespace Amuse.Domain.Discovery;

public sealed class LikedTrack
{
    public ListenerProfileId ListenerProfileId { get; private set; }
    public TrackId TrackId { get; private set; }
    public DateTimeOffset LikedAt { get; private set; }

    private LikedTrack()
    {
    }

    public static LikedTrack Create(ListenerProfileId listenerId, TrackId trackId, DateTimeOffset now) =>
        new()
        {
            ListenerProfileId = listenerId,
            TrackId = trackId,
            LikedAt = now,
        };

    public static LikedTrack Rehydrate(
        ListenerProfileId listenerId,
        TrackId trackId,
        DateTimeOffset likedAt) =>
        new()
        {
            ListenerProfileId = listenerId,
            TrackId = trackId,
            LikedAt = likedAt,
        };

    public void RefreshLike(DateTimeOffset now) => LikedAt = now;
}
