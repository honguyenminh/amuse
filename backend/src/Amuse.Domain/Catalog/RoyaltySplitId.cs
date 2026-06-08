namespace Amuse.Domain.Catalog;

public readonly record struct RoyaltySplitId(Guid Value)
{
    public static RoyaltySplitId New() => new(Guid.CreateVersion7());

    public static RoyaltySplitId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("Royalty split id cannot be empty.", nameof(value));

        return new RoyaltySplitId(value);
    }
}
