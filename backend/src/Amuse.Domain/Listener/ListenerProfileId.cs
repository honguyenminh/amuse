namespace Amuse.Domain.Listener;

public readonly record struct ListenerProfileId(Guid Value)
{
    public static ListenerProfileId New() => new(Guid.CreateVersion7());

    public static ListenerProfileId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("Listener profile id cannot be empty.", nameof(value));

        return new ListenerProfileId(value);
    }
}
