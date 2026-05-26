namespace Amuse.Domain.Catalog;

public sealed class Album
{
    public const int MaxTitleLength = 300;
    public const int MaxKeyLength = 512;

    public AlbumId Id { get; private set; }
    public ArtistId ArtistId { get; private set; }
    public string Title { get; private set; } = null!;
    public Slug Slug { get; private set; }
    public ReleaseType ReleaseType { get; private set; }
    public DateTimeOffset ReleaseDate { get; private set; }
    public string? CoverArtKey { get; private set; }
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
        string? coverArtKey,
        DateTimeOffset createdAt)
    {
        Id = id;
        ArtistId = artistId;
        Title = title;
        Slug = slug;
        ReleaseType = releaseType;
        ReleaseDate = releaseDate;
        CoverArtKey = coverArtKey;
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
        string? coverArtKey = null)
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

        if (coverArtKey is { Length: > MaxKeyLength })
            throw new ArgumentException(
                $"Cover art key exceeds {MaxKeyLength} characters.",
                nameof(coverArtKey));

        return new Album(id, artistId, trimmedTitle, slug, releaseType, releaseDate, coverArtKey, createdAt);
    }

    public Track AddTrack(
        TrackId id,
        string title,
        int trackNumber,
        TrackDuration duration,
        string? audioMasterKey = null)
    {
        if (_tracks.Any(t => t.TrackNumber == trackNumber))
            throw new InvalidOperationException(
                $"Track number {trackNumber} already exists on album '{Id.Value}'.");

        var track = new Track(id, Id, title, trackNumber, duration, audioMasterKey);
        _tracks.Add(track);
        return track;
    }
}
