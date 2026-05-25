namespace Amuse.Domain.Catalog;

public readonly record struct TrackId(Guid Value)
{
    public static TrackId New() => new(Guid.CreateVersion7());

    public static TrackId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("Track id cannot be empty.", nameof(value));

        return new TrackId(value);
    }
}
