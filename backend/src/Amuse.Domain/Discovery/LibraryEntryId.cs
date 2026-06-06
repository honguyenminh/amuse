namespace Amuse.Domain.Discovery;

public readonly record struct LibraryEntryId(Guid Value)
{
    public static LibraryEntryId New() => new(Guid.CreateVersion7());

    public static LibraryEntryId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("Library entry id cannot be empty.", nameof(value));

        return new LibraryEntryId(value);
    }
}
