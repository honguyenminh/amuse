namespace Amuse.Domain.Catalog;

public sealed class Album
{
    public const int MaxTitleLength = 300;
    public const int MaxUrlLength = 1024;

    public AlbumId Id { get; private set; }
    public ArtistId ArtistId { get; private set; }
    public string Title { get; private set; } = null!;
    public Slug Slug { get; private set; }
    public ReleaseType ReleaseType { get; private set; }
    public DateTimeOffset ReleaseDate { get; private set; }
    public string? CoverArtUrl { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private readonly List<Track> _tracks = [];
    public IReadOnlyList<Track> Tracks => _tracks;

    private Album()
    {
    }

    private Album(
        AlbumId id,
        ArtistId artistId,
        string title,
        Slug slug,
        ReleaseType releaseType,
        DateTimeOffset releaseDate,
        string? coverArtUrl,
        DateTimeOffset createdAt)
    {
        Id = id;
        ArtistId = artistId;
        Title = title;
        Slug = slug;
        ReleaseType = releaseType;
        ReleaseDate = releaseDate;
        CoverArtUrl = coverArtUrl;
        CreatedAt = createdAt;
    }

    public static Album Create(
        AlbumId id,
        ArtistId artistId,
        string title,
        Slug slug,
        ReleaseType releaseType,
        DateTimeOffset releaseDate,
        DateTimeOffset createdAt,
        string? coverArtUrl = null)
    {
        var trimmedTitle = (title ?? throw new ArgumentNullException(nameof(title))).Trim();
        if (trimmedTitle.Length is 0 or > MaxTitleLength)
            throw new ArgumentException(
                $"Album title must be 1..{MaxTitleLength} characters.",
                nameof(title));

        if (releaseDate.Offset != TimeSpan.Zero)
            throw new ArgumentException(
                "Release date must be expressed in UTC (offset 0).",
                nameof(releaseDate));

        if (coverArtUrl is { Length: > MaxUrlLength })
            throw new ArgumentException(
                $"Cover art url exceeds {MaxUrlLength} characters.",
                nameof(coverArtUrl));

        return new Album(id, artistId, trimmedTitle, slug, releaseType, releaseDate, coverArtUrl, createdAt);
    }

    public Track AddTrack(
        TrackId id,
        string title,
        int trackNumber,
        TrackDuration duration,
        string? audioUrl = null)
    {
        if (_tracks.Any(t => t.TrackNumber == trackNumber))
            throw new InvalidOperationException(
                $"Track number {trackNumber} already exists on album '{Id.Value}'.");

        var track = new Track(id, Id, title, trackNumber, duration, audioUrl);
        _tracks.Add(track);
        return track;
    }
}
