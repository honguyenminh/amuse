namespace Amuse.Domain.Catalog;

public readonly record struct ArtistId(Guid Value)
{
    public static ArtistId New() => new(Guid.CreateVersion7());

    public static ArtistId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("Artist id cannot be empty.", nameof(value));

        return new ArtistId(value);
    }
}
