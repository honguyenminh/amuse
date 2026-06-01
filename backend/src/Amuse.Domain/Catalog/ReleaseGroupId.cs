namespace Amuse.Domain.Catalog;

public readonly record struct ReleaseGroupId(Guid Value)
{
    public static ReleaseGroupId New() => new(Guid.CreateVersion7());

    public static ReleaseGroupId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("Release group id cannot be empty.", nameof(value));

        return new ReleaseGroupId(value);
    }
}
