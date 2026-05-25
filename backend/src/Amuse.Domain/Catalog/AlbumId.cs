namespace Amuse.Domain.Catalog;

public readonly record struct AlbumId(Guid Value)
{
    public static AlbumId New() => new(Guid.CreateVersion7());

    public static AlbumId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("Album id cannot be empty.", nameof(value));

        return new AlbumId(value);
    }
}
