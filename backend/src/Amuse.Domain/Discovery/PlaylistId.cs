namespace Amuse.Domain.Discovery;

public readonly record struct PlaylistId(Guid Value)
{
    public static PlaylistId New() => new(Guid.CreateVersion7());

    public static PlaylistId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("Playlist id cannot be empty.", nameof(value));

        return new PlaylistId(value);
    }
}
