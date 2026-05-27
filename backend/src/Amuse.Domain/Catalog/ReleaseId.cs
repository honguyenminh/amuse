namespace Amuse.Domain.Catalog;

public readonly record struct ReleaseId(Guid Value)
{
    public static ReleaseId New() => new(Guid.CreateVersion7());

    public static ReleaseId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("Release id cannot be empty.", nameof(value));

        return new ReleaseId(value);
    }
}
