namespace Amuse.Domain.Platform;

public readonly record struct PlatformOperatorId(int Value)
{
    public static PlatformOperatorId From(int value)
    {
        if (value <= 0)
            throw new ArgumentException("Platform operator id must be positive.", nameof(value));

        return new PlatformOperatorId(value);
    }

    public static PlatformOperatorId Root => new(1);
}
