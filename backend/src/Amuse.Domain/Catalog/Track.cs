using Amuse.Domain.SharedKernel;
using Amuse.Domain.Tenancy;

namespace Amuse.Domain.Catalog;

public sealed class Track
{
    public const int MaxTitleLength = 300;
    public const int MaxKeyLength = 512;
    public const int MaxIsrcLength = 32;
    public const int MaxLyricsLength = 12000;
    public const int MaxLanguageCodeLength = 12;
    public const int MaxVersionTitleLength = 200;
    public const int MaxCreditsLength = 4000;

    public TrackId Id { get; private set; }
    public OrganizationId OrganizationId { get; private set; }
    public ReleaseId ReleaseId { get; private set; }
    public string Title { get; private set; } = null!;
    public int TrackNumber { get; private set; }
    public TrackDuration Duration { get; private set; }
    public bool ExplicitFlag { get; private set; }
    public string? Isrc { get; private set; }
    public string? Lyrics { get; private set; }
    public string? LanguageCode { get; private set; }
    public string? VersionTitle { get; private set; }
    public string? ComposerCredits { get; private set; }
    public TrackLifecycleStatus LifecycleStatus { get; private set; }
    public string? AudioMasterKey { get; private set; }
    public string? AudioStreamKey { get; private set; }

    private Track()
    {
    }

    internal Track(
        TrackId id,
        OrganizationId organizationId,
        ReleaseId releaseId,
        string title,
        int trackNumber,
        TrackDuration duration,
        bool explicitFlag,
        string? audioMasterKey,
        string? isrc = null,
        string? lyrics = null,
        string? languageCode = null,
        string? versionTitle = null,
        string? composerCredits = null,
        TrackLifecycleStatus lifecycleStatus = TrackLifecycleStatus.Draft,
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

        if (!ValidateOptional(isrc, MaxIsrcLength)
            || !ValidateOptional(lyrics, MaxLyricsLength)
            || !ValidateOptional(languageCode, MaxLanguageCodeLength)
            || !ValidateOptional(versionTitle, MaxVersionTitleLength)
            || !ValidateOptional(composerCredits, MaxCreditsLength))
        {
            throw new ArgumentException("Track metadata is invalid.");
        }

        Id = id;
        OrganizationId = organizationId;
        ReleaseId = releaseId;
        Title = trimmedTitle;
        TrackNumber = trackNumber;
        Duration = duration;
        ExplicitFlag = explicitFlag;
        Isrc = NormalizeOptional(isrc);
        Lyrics = NormalizeOptional(lyrics);
        LanguageCode = NormalizeOptional(languageCode);
        VersionTitle = NormalizeOptional(versionTitle);
        ComposerCredits = NormalizeOptional(composerCredits);
        LifecycleStatus = lifecycleStatus;
        AudioMasterKey = audioMasterKey;
        AudioStreamKey = audioStreamKey;
    }

    public Result UpdateMetadata(
        string title,
        int trackNumber,
        bool explicitFlag,
        string? isrc,
        string? lyrics,
        string? languageCode,
        string? versionTitle,
        string? composerCredits)
    {
        if (LifecycleStatus is TrackLifecycleStatus.Processing or TrackLifecycleStatus.Published)
            return Result.Failure(CatalogErrors.InvalidLifecycleTransition);

        var trimmedTitle = (title ?? throw new ArgumentNullException(nameof(title))).Trim();
        if (trimmedTitle.Length is 0 or > MaxTitleLength)
            return Result.Failure(CatalogErrors.InvalidTrack);

        if (trackNumber <= 0)
            return Result.Failure(CatalogErrors.InvalidTrack);

        if (!ValidateOptional(isrc, MaxIsrcLength)
            || !ValidateOptional(lyrics, MaxLyricsLength)
            || !ValidateOptional(languageCode, MaxLanguageCodeLength)
            || !ValidateOptional(versionTitle, MaxVersionTitleLength)
            || !ValidateOptional(composerCredits, MaxCreditsLength))
        {
            return Result.Failure(CatalogErrors.InvalidTrack);
        }

        Title = trimmedTitle;
        TrackNumber = trackNumber;
        ExplicitFlag = explicitFlag;
        Isrc = NormalizeOptional(isrc);
        Lyrics = NormalizeOptional(lyrics);
        LanguageCode = NormalizeOptional(languageCode);
        VersionTitle = NormalizeOptional(versionTitle);
        ComposerCredits = NormalizeOptional(composerCredits);
        return Result.Success();
    }

    public Result SetDurationFromUploadedAudio(TrackDuration duration)
    {
        if (LifecycleStatus is TrackLifecycleStatus.Published or TrackLifecycleStatus.Hidden)
            return Result.Failure(CatalogErrors.InvalidLifecycleTransition);

        Duration = duration;
        return Result.Success();
    }

    public Result MarkProcessing()
    {
        if (LifecycleStatus is not (TrackLifecycleStatus.Draft or TrackLifecycleStatus.Ready))
            return Result.Failure(CatalogErrors.InvalidLifecycleTransition);

        LifecycleStatus = TrackLifecycleStatus.Processing;
        return Result.Success();
    }

    public Result MarkReady()
    {
        if (LifecycleStatus is TrackLifecycleStatus.Hidden)
            return Result.Failure(CatalogErrors.InvalidLifecycleTransition);

        if (string.IsNullOrWhiteSpace(AudioStreamKey))
            return Result.Failure(CatalogErrors.TrackStreamNotReady);

        LifecycleStatus = TrackLifecycleStatus.Ready;
        return Result.Success();
    }

    internal void Publish()
    {
        if (LifecycleStatus == TrackLifecycleStatus.Ready)
            LifecycleStatus = TrackLifecycleStatus.Published;
    }

    internal void Hide()
    {
        if (LifecycleStatus == TrackLifecycleStatus.Published)
            LifecycleStatus = TrackLifecycleStatus.Hidden;
    }

    internal void MarkPublishedForDevelopment()
    {
        LifecycleStatus = TrackLifecycleStatus.Published;
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

    private static bool ValidateOptional(string? value, int maxLength)
    {
        var normalized = NormalizeOptional(value);
        return normalized is null || normalized.Length <= maxLength;
    }

    private static string? NormalizeOptional(string? value)
    {
        if (value is null)
            return null;
        var trimmed = value.Trim();
        return trimmed.Length == 0 ? null : trimmed;
    }
}
