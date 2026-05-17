namespace Amuse.Domain.Identity;

public readonly record struct RefreshSessionId(Guid Value)
{
    public static RefreshSessionId New() => new(Guid.CreateVersion7());

    public static RefreshSessionId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("Refresh session id cannot be empty.", nameof(value));

        return new RefreshSessionId(value);
    }
}
