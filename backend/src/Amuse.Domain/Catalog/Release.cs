using Amuse.Domain.SharedKernel;
using Amuse.Domain.Tenancy;

namespace Amuse.Domain.Catalog;

public sealed class Release
{
    public const int MaxTitleLength = 300;
    public const int MaxKeyLength = 512;
    public const int MaxDescriptionLength = CatalogFormattedText.MaxLength;
    public const int MaxUpcLength = 32;
    public const int MaxGenreLength = 100;
    public const int MaxTagsLength = 500;
    public const int MaxLanguageCodeLength = 12;
    public const int MaxLabelNameLength = 300;
    public const int MaxRightsLineLength = 500;

    public ReleaseId Id { get; private set; }
    public OrganizationId OrganizationId { get; private set; }
    public ArtistId ArtistId { get; private set; }
    public ReleaseGroupId? ReleaseGroupId { get; private set; }
    public string Title { get; private set; } = null!;
    public Slug Slug { get; private set; }
    public ReleaseType ReleaseType { get; private set; }
    public ReleaseLifecycleStatus LifecycleStatus { get; private set; }
    public DateTimeOffset ReleaseDate { get; private set; }
    public string? Description { get; private set; }
    public string? Upc { get; private set; }
    public string? PrimaryGenre { get; private set; }
    public string? Tags { get; private set; }
    public string? LanguageCode { get; private set; }
    public string? LabelName { get; private set; }
    public string? PLine { get; private set; }
    public string? CLine { get; private set; }
    public DateTimeOffset? OriginalReleaseDate { get; private set; }
    public bool MetadataComplete { get; private set; }
    public string? CoverArtKey { get; private set; }
    public DateTimeOffset? PublishedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private readonly List<Track> _tracks = [];
    public IReadOnlyList<Track> Tracks => _tracks;

    private readonly List<ReleaseCollaborator> _collaborators = [];
    public IReadOnlyList<ReleaseCollaborator> Collaborators => _collaborators;

    private Release()
    {
    }

    private Release(
        ReleaseId id,
        OrganizationId organizationId,
        ArtistId artistId,
        ReleaseGroupId? releaseGroupId,
        string title,
        Slug slug,
        ReleaseType releaseType,
        ReleaseLifecycleStatus lifecycleStatus,
        DateTimeOffset releaseDate,
        string? description,
        string? upc,
        string? primaryGenre,
        string? tags,
        string? languageCode,
        string? labelName,
        string? pLine,
        string? cLine,
        DateTimeOffset? originalReleaseDate,
        bool metadataComplete,
        string? coverArtKey,
        DateTimeOffset createdAt)
    {
        Id = id;
        OrganizationId = organizationId;
        ArtistId = artistId;
        ReleaseGroupId = releaseGroupId;
        Title = title;
        Slug = slug;
        ReleaseType = releaseType;
        LifecycleStatus = lifecycleStatus;
        ReleaseDate = releaseDate;
        Description = description;
        Upc = upc;
        PrimaryGenre = primaryGenre;
        Tags = tags;
        LanguageCode = languageCode;
        LabelName = labelName;
        PLine = pLine;
        CLine = cLine;
        OriginalReleaseDate = originalReleaseDate;
        MetadataComplete = metadataComplete;
        CoverArtKey = coverArtKey;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public static Result<Release> Create(
        ReleaseId id,
        OrganizationId organizationId,
        ArtistId artistId,
        string title,
        Slug slug,
        ReleaseType releaseType,
        DateTimeOffset releaseDate,
        DateTimeOffset createdAt,
        ReleaseGroupId? releaseGroupId = null,
        string? description = null,
        string? upc = null,
        string? primaryGenre = null,
        string? tags = null,
        string? languageCode = null,
        string? labelName = null,
        string? pLine = null,
        string? cLine = null,
        DateTimeOffset? originalReleaseDate = null,
        bool metadataComplete = false,
        string? coverArtKey = null)
    {
        var trimmedTitle = title.Trim();
        if (trimmedTitle.Length is 0 or > MaxTitleLength)
            return Result<Release>.Failure(CatalogErrors.InvalidRelease);

        if (releaseDate.Offset != TimeSpan.Zero)
            return Result<Release>.Failure(CatalogErrors.InvalidRelease);

        if (coverArtKey is { Length: > MaxKeyLength })
            return Result<Release>.Failure(CatalogErrors.InvalidRelease);

        var metadataResult = ValidateMetadataFields(
            description,
            upc,
            primaryGenre,
            tags,
            languageCode,
            labelName,
            pLine,
            cLine,
            originalReleaseDate);
        if (!metadataResult.IsSuccess)
            return Result<Release>.Failure(metadataResult.Error!);

        var descriptionResult = CatalogFormattedText.TryCreate(description);
        if (!descriptionResult.IsSuccess)
            return Result<Release>.Failure(descriptionResult.Error!);

        return Result<Release>.Success(
            new Release(
                id,
                organizationId,
                artistId,
                releaseGroupId,
                trimmedTitle,
                slug,
                releaseType,
                ReleaseLifecycleStatus.Draft,
                releaseDate,
                CatalogFormattedText.ToStoredValue(descriptionResult.Value),
                NormalizeOptional(upc),
                NormalizeOptional(primaryGenre),
                NormalizeOptional(tags),
                NormalizeOptional(languageCode),
                NormalizeOptional(labelName),
                NormalizeOptional(pLine),
                NormalizeOptional(cLine),
                originalReleaseDate,
                metadataComplete,
                coverArtKey,
                createdAt));
    }

    public Result UpdateMetadata(
        string title,
        ReleaseType releaseType,
        DateTimeOffset releaseDate,
        ReleaseGroupId? releaseGroupId,
        string? description,
        string? upc,
        string? primaryGenre,
        string? tags,
        string? languageCode,
        string? labelName,
        string? pLine,
        string? cLine,
        DateTimeOffset? originalReleaseDate,
        bool metadataComplete,
        DateTimeOffset now)
    {
        if (LifecycleStatus is ReleaseLifecycleStatus.Published or ReleaseLifecycleStatus.Archived)
            return Result.Failure(CatalogErrors.InvalidLifecycleTransition);

        var trimmedTitle = (title ?? throw new ArgumentNullException(nameof(title))).Trim();
        if (trimmedTitle.Length is 0 or > MaxTitleLength)
            return Result.Failure(CatalogErrors.InvalidRelease);

        if (releaseDate.Offset != TimeSpan.Zero)
            return Result.Failure(CatalogErrors.InvalidRelease);

        var metadataResult = ValidateMetadataFields(
            description,
            upc,
            primaryGenre,
            tags,
            languageCode,
            labelName,
            pLine,
            cLine,
            originalReleaseDate);
        if (!metadataResult.IsSuccess)
            return Result.Failure(metadataResult.Error!);

        var descriptionResult = CatalogFormattedText.TryCreate(description);
        if (!descriptionResult.IsSuccess)
            return Result.Failure(descriptionResult.Error!);

        Title = trimmedTitle;
        ReleaseType = releaseType;
        ReleaseDate = releaseDate;
        ReleaseGroupId = releaseGroupId;
        Description = CatalogFormattedText.ToStoredValue(descriptionResult.Value);
        Upc = NormalizeOptional(upc);
        PrimaryGenre = NormalizeOptional(primaryGenre);
        Tags = NormalizeOptional(tags);
        LanguageCode = NormalizeOptional(languageCode);
        LabelName = NormalizeOptional(labelName);
        PLine = NormalizeOptional(pLine);
        CLine = NormalizeOptional(cLine);
        OriginalReleaseDate = originalReleaseDate;
        MetadataComplete = metadataComplete;
        UpdatedAt = now;
        return Result.Success();
    }

    public Result UpdateSlug(Slug slug, DateTimeOffset now)
    {
        if (LifecycleStatus is ReleaseLifecycleStatus.Published or ReleaseLifecycleStatus.Archived)
            return Result.Failure(CatalogErrors.InvalidLifecycleTransition);

        Slug = slug;
        UpdatedAt = now;
        return Result.Success();
    }

    public void SetCoverArtKey(string coverArtKey, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(coverArtKey) || coverArtKey.Length > MaxKeyLength)
            throw new ArgumentException("Cover art key is invalid.", nameof(coverArtKey));

        CoverArtKey = coverArtKey;
        UpdatedAt = now;
    }

    public Result Schedule(DateTimeOffset now)
    {
        if (LifecycleStatus != ReleaseLifecycleStatus.Draft)
            return Result.Failure(CatalogErrors.InvalidLifecycleTransition);

        if (!AllTracksReadyForPublish())
            return Result.Failure(CatalogErrors.ReleaseNotReadyToSchedule);

        if (ReleaseDate <= now)
            return Result.Failure(CatalogErrors.ReleaseDateNotInFuture);

        LifecycleStatus = ReleaseLifecycleStatus.Scheduled;
        UpdatedAt = now;
        return Result.Success();
    }

    public Result CancelSchedule(DateTimeOffset now)
    {
        if (LifecycleStatus != ReleaseLifecycleStatus.Scheduled)
            return Result.Failure(CatalogErrors.InvalidLifecycleTransition);

        LifecycleStatus = ReleaseLifecycleStatus.Draft;
        UpdatedAt = now;
        return Result.Success();
    }

    public Result Publish(DateTimeOffset now)
    {
        if (LifecycleStatus is ReleaseLifecycleStatus.Published)
            return Result.Failure(CatalogErrors.InvalidLifecycleTransition);

        if (LifecycleStatus is not (ReleaseLifecycleStatus.Draft or ReleaseLifecycleStatus.Scheduled))
            return Result.Failure(CatalogErrors.InvalidLifecycleTransition);

        if (_tracks.Count == 0)
            return Result.Failure(CatalogErrors.ReleaseHasNoTracks);

        if (!AllTracksReadyForPublish())
            return Result.Failure(CatalogErrors.TracksNotReady);

        foreach (var track in _tracks)
            track.Publish();

        LifecycleStatus = ReleaseLifecycleStatus.Published;
        PublishedAt = now;
        UpdatedAt = now;
        return Result.Success();
    }

    public Result Hide(DateTimeOffset now)
    {
        if (LifecycleStatus == ReleaseLifecycleStatus.Archived)
            return Result.Failure(CatalogErrors.InvalidLifecycleTransition);

        LifecycleStatus = ReleaseLifecycleStatus.Hidden;
        UpdatedAt = now;
        foreach (var track in _tracks)
            track.Hide();

        return Result.Success();
    }

    public bool CanBeDeleted() =>
        LifecycleStatus is ReleaseLifecycleStatus.Draft
            or ReleaseLifecycleStatus.Processing
            or ReleaseLifecycleStatus.Ready
            or ReleaseLifecycleStatus.Scheduled;

    public Result<Track> RemoveTrack(TrackId trackId, DateTimeOffset now)
    {
        if (!CanBeDeleted())
            return Result<Track>.Failure(CatalogErrors.ReleaseNotDeletable);

        var track = _tracks.FirstOrDefault(t => t.Id == trackId);
        if (track is null)
            return Result<Track>.Failure(CatalogErrors.TrackNotFound);

        _tracks.Remove(track);
        UpdatedAt = now;
        return Result<Track>.Success(track);
    }

    public Result<Track> UpdateTrack(
        TrackId trackId,
        string title,
        int trackNumber,
        bool explicitFlag,
        string? isrc,
        string? lyrics,
        string? languageCode,
        string? versionTitle,
        string? composerCredits,
        DateTimeOffset now)
    {
        var track = _tracks.FirstOrDefault(t => t.Id == trackId);
        if (track is null)
            return Result<Track>.Failure(CatalogErrors.TrackNotFound);

        if (_tracks.Any(t => t.TrackNumber == trackNumber && t.Id != trackId))
            return Result<Track>.Failure(CatalogErrors.DuplicateTrackNumber);

        var updateResult = track.UpdateMetadata(
            title,
            trackNumber,
            explicitFlag,
            isrc,
            lyrics,
            languageCode,
            versionTitle,
            composerCredits);

        if (!updateResult.IsSuccess)
            return Result<Track>.Failure(updateResult.Error!);

        UpdatedAt = now;
        return Result<Track>.Success(track);
    }

    public Result ReplaceCollaborators(IReadOnlyList<ArtistId> collaboratorArtistIds)
    {
        var ids = collaboratorArtistIds
            .Where(id => id.Value != Guid.Empty)
            .Distinct()
            .ToArray();

        if (ids.Any(id => id == ArtistId))
            return Result.Failure(CatalogErrors.InvalidCollaborator);

        _collaborators.Clear();

        var order = 1;
        foreach (var artistId in ids)
        {
            var createResult = ReleaseCollaborator.Create(
                Id,
                artistId,
                ArtistId,
                ReleaseCollaboratorRole.Featured,
                order);

            if (!createResult.IsSuccess)
                return Result.Failure(createResult.Error!);

            _collaborators.Add(createResult.Value!);
            order++;
        }

        return Result.Success();
    }

    public Result<Track> AddTrack(
        TrackId id,
        string title,
        int trackNumber,
        TrackDuration duration,
        bool explicitFlag = false,
        string? isrc = null,
        string? lyrics = null,
        string? languageCode = null,
        string? versionTitle = null,
        string? composerCredits = null,
        string? audioMasterKey = null)
    {
        if (LifecycleStatus is ReleaseLifecycleStatus.Published or ReleaseLifecycleStatus.Hidden
            or ReleaseLifecycleStatus.Archived or ReleaseLifecycleStatus.Scheduled)
            return Result<Track>.Failure(CatalogErrors.InvalidLifecycleTransition);

        if (_tracks.Any(t => t.TrackNumber == trackNumber))
            return Result<Track>.Failure(CatalogErrors.DuplicateTrackNumber);

        var track = new Track(
            id,
            OrganizationId,
            Id,
            title,
            trackNumber,
            duration,
            explicitFlag,
            audioMasterKey,
            isrc,
            lyrics,
            languageCode,
            versionTitle,
            composerCredits);

        _tracks.Add(track);
        UpdatedAt = DateTimeOffset.UtcNow;
        return Result<Track>.Success(track);
    }

    public void MarkPublishedForDevelopment(DateTimeOffset now)
    {
        LifecycleStatus = ReleaseLifecycleStatus.Published;
        PublishedAt = now;
        UpdatedAt = now;
        foreach (var track in _tracks)
            track.MarkPublishedForDevelopment();
    }

    private bool AllTracksReadyForPublish() =>
        _tracks.Count > 0
        && _tracks.All(t => t.LifecycleStatus is TrackLifecycleStatus.Ready or TrackLifecycleStatus.Published);

    private static Result ValidateMetadataFields(
        string? description,
        string? upc,
        string? primaryGenre,
        string? tags,
        string? languageCode,
        string? labelName,
        string? pLine,
        string? cLine,
        DateTimeOffset? originalReleaseDate)
    {
        if (NormalizeOptional(upc)?.Length > MaxUpcLength
            || NormalizeOptional(primaryGenre)?.Length > MaxGenreLength
            || NormalizeOptional(tags)?.Length > MaxTagsLength
            || NormalizeOptional(languageCode)?.Length > MaxLanguageCodeLength
            || NormalizeOptional(labelName)?.Length > MaxLabelNameLength
            || NormalizeOptional(pLine)?.Length > MaxRightsLineLength
            || NormalizeOptional(cLine)?.Length > MaxRightsLineLength)
        {
            return Result.Failure(CatalogErrors.InvalidRelease);
        }

        if (originalReleaseDate.HasValue && originalReleaseDate.Value.Offset != TimeSpan.Zero)
            return Result.Failure(CatalogErrors.InvalidRelease);

        return Result.Success();
    }

    private static string? NormalizeOptional(string? value)
    {
        if (value is null)
            return null;
        var trimmed = value.Trim();
        return trimmed.Length == 0 ? null : trimmed;
    }
}
