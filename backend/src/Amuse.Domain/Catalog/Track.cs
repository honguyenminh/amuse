namespace Amuse.Domain.Catalog;

public sealed class Track
{
    public const int MaxTitleLength = 300;
    public const int MaxKeyLength = 512;

    public TrackId Id { get; private set; }
    public ReleaseId ReleaseId { get; private set; }
    public string Title { get; private set; } = null!;
    public int TrackNumber { get; private set; }
    public TrackDuration Duration { get; private set; }
    public string? AudioMasterKey { get; private set; }
    public string? AudioStreamKey { get; private set; }

    private Track()
    {
    }

    internal Track(
        TrackId id,
        ReleaseId releaseId,
        string title,
        int trackNumber,
        TrackDuration duration,
        string? audioMasterKey,
        string? audioStreamKey = null)
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

        if (audioMasterKey is { Length: > MaxKeyLength })
            throw new ArgumentException(
                $"Audio master key exceeds {MaxKeyLength} characters.",
                nameof(audioMasterKey));

        if (audioStreamKey is { Length: > MaxKeyLength })
            throw new ArgumentException(
                $"Audio stream key exceeds {MaxKeyLength} characters.",
                nameof(audioStreamKey));

        Id = id;
        ReleaseId = releaseId;
        Title = trimmedTitle;
        TrackNumber = trackNumber;
        Duration = duration;
        AudioMasterKey = audioMasterKey;
        AudioStreamKey = audioStreamKey;
    }

    public void SetAudioMaster(string audioMasterKey)
    {
        if (string.IsNullOrWhiteSpace(audioMasterKey))
            throw new ArgumentException("Audio master key is required.", nameof(audioMasterKey));
        if (audioMasterKey.Length > MaxKeyLength)
            throw new ArgumentException($"Audio master key exceeds {MaxKeyLength} characters.", nameof(audioMasterKey));
        AudioMasterKey = audioMasterKey;
    }

    public void SetAudioStream(string audioStreamKey)
    {
        if (string.IsNullOrWhiteSpace(audioStreamKey))
            throw new ArgumentException("Audio stream key is required.", nameof(audioStreamKey));
        if (audioStreamKey.Length > MaxKeyLength)
            throw new ArgumentException($"Audio stream key exceeds {MaxKeyLength} characters.", nameof(audioStreamKey));
        AudioStreamKey = audioStreamKey;
    }
}
