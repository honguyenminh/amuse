namespace Amuse.Domain.Discovery;

public readonly record struct PlaylistItemId(Guid Value)
{
    public static PlaylistItemId New() => new(Guid.CreateVersion7());

    public static PlaylistItemId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("Playlist item id cannot be empty.", nameof(value));

        return new PlaylistItemId(value);
    }
}
