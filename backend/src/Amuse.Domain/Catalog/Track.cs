namespace Amuse.Domain.Catalog;

public sealed class Track
{
    public const int MaxTitleLength = 300;
    public const int MaxUrlLength = 1024;

    public TrackId Id { get; private set; }
    public AlbumId AlbumId { get; private set; }
    public string Title { get; private set; } = null!;
    public int TrackNumber { get; private set; }
    public TrackDuration Duration { get; private set; }
    public string? AudioUrl { get; private set; }

    private Track()
    {
    }

    internal Track(
        TrackId id,
        AlbumId albumId,
        string title,
        int trackNumber,
        TrackDuration duration,
        string? audioUrl)
    {
        var trimmedTitle = (title ?? throw new ArgumentNullException(nameof(title))).Trim();
        if (trimmedTitle.Length is 0 or > MaxTitleLength)
            throw new ArgumentException(
                $"Track title must be 1..{MaxTitleLength} characters.",
                nameof(title));

        if (trackNumber <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(trackNumber),
                trackNumber,
                "Track number must be positive.");

        if (audioUrl is { Length: > MaxUrlLength })
            throw new ArgumentException($"Audio url exceeds {MaxUrlLength} characters.", nameof(audioUrl));

        Id = id;
        AlbumId = albumId;
        Title = trimmedTitle;
        TrackNumber = trackNumber;
        Duration = duration;
        AudioUrl = audioUrl;
    }
}
