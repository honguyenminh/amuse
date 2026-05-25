namespace Amuse.Domain.Catalog;

/// <summary>
/// Track length in whole milliseconds. Must be strictly positive.
/// </summary>
public readonly record struct TrackDuration
{
    public int Milliseconds { get; }

    private TrackDuration(int milliseconds) => Milliseconds = milliseconds;

    public static TrackDuration FromMilliseconds(int milliseconds)
    {
        if (milliseconds <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(milliseconds),
                milliseconds,
                "Track duration must be positive.");
        return new TrackDuration(milliseconds);
    }

    public static TrackDuration FromTimeSpan(TimeSpan timeSpan) =>
        FromMilliseconds((int)timeSpan.TotalMilliseconds);

    public TimeSpan ToTimeSpan() => TimeSpan.FromMilliseconds(Milliseconds);
}
