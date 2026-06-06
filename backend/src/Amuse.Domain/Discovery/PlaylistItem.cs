using Amuse.Domain.Catalog;

namespace Amuse.Domain.Discovery;

public sealed class PlaylistItem
{
    public PlaylistItemId Id { get; private set; }
    public TrackId TrackId { get; private set; }
    public int Position { get; private set; }
    public DateTimeOffset AddedAt { get; private set; }

    private PlaylistItem()
    {
    }

    internal static PlaylistItem Create(TrackId trackId, int position, DateTimeOffset addedAt) =>
        new()
        {
            Id = PlaylistItemId.New(),
            TrackId = trackId,
            Position = position,
            AddedAt = addedAt,
        };

    internal static PlaylistItem Rehydrate(
        PlaylistItemId id,
        TrackId trackId,
        int position,
        DateTimeOffset addedAt) =>
        new()
        {
            Id = id,
            TrackId = trackId,
            Position = position,
            AddedAt = addedAt,
        };

    internal void SetPosition(int position) => Position = position;
}
