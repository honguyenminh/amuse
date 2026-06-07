using Amuse.Domain.Catalog;
using Amuse.Modules.Catalog.Processing;

namespace Amuse.Modules.Catalog.Features.Common;

public sealed record CreateReleaseGroupRequest(string Title, string? Description);

public sealed record UpdateReleaseGroupRequest(string Title, string? Description);

public sealed record ManageReleaseGroupResponse(
    Guid Id,
    string Slug,
    string Title,
    string? Description,
    Guid ArtistId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record ManageReleaseGroupSummaryResponse(
    Guid Id,
    string Slug,
    string Title,
    string? Description,
    int ReleaseCount,
    DateTimeOffset UpdatedAt);

public sealed record ManageReleaseGroupMemberResponse(
    Guid Id,
    string Slug,
    string Title,
    ReleaseType ReleaseType,
    ReleaseLifecycleStatus LifecycleStatus,
    DateTimeOffset ReleaseDate,
    string? CoverArtUrl);

public sealed record ManageReleaseGroupDetailResponse(
    Guid Id,
    string Slug,
    string Title,
    string? Description,
    Guid ArtistId,
    string ArtistName,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<ManageReleaseGroupMemberResponse> Releases);

public sealed record ManageReleaseGroupListResponse(
    IReadOnlyList<ManageReleaseGroupResponse> Items);

public sealed record CreateArtistRequest(
    string Name,
    string Slug,
    string? Bio = null,
    string? CountryCode = null,
    string? WebsiteUrl = null,
    string? Aliases = null);

public sealed record UpdateArtistRequest(
    string Name,
    string? Bio = null,
    string? CountryCode = null,
    string? WebsiteUrl = null,
    string? Aliases = null);

public sealed record ManageArtistSummaryResponse(
    Guid Id,
    string Slug,
    string Name,
    ArtistVisibilityTier VisibilityTier,
    DateTimeOffset CreatedAt);

public sealed record ManageArtistListResponse(
    IReadOnlyList<ManageArtistSummaryResponse> Items);

public sealed record ArtistSlugAvailabilityResponse(
    string NormalizedSlug,
    bool IsValid,
    bool IsAvailable);

public sealed record ReleaseSlugAvailabilityResponse(
    string NormalizedSlug,
    bool IsValid,
    bool IsAvailable);

public sealed record ManageArtistReleaseSummary(
    Guid Id,
    string Slug,
    string Title,
    ReleaseType ReleaseType,
    ReleaseLifecycleStatus LifecycleStatus,
    DateTimeOffset ReleaseDate,
    string? CoverArtUrl);

public sealed record ManageArtistTrackSummary(
    Guid Id,
    string Title,
    int TrackNumber,
    int DurationMs,
    bool ExplicitFlag,
    TrackLifecycleStatus LifecycleStatus);

public sealed record ManageArtistDetailResponse(
    Guid Id,
    string Slug,
    string Name,
    string? Bio,
    string? CountryCode,
    string? WebsiteUrl,
    string? Aliases,
    string? AvatarUrl,
    string? CoverUrl,
    ArtistVisibilityTier VisibilityTier,
    DateTimeOffset CreatedAt,
    IReadOnlyList<ManageArtistReleaseSummary> Releases,
    IReadOnlyList<ManageArtistTrackSummary> Tracks,
    IReadOnlyList<ManageReleaseGroupSummaryResponse> ReleaseGroups);

public sealed record ManageReleaseCollaboratorResponse(
    Guid ArtistId,
    string ArtistName,
    ReleaseCollaboratorRole Role,
    int DisplayOrder);

public sealed record CreateReleaseRequest(
    string Title,
    ReleaseType ReleaseType,
    DateTimeOffset ReleaseDate,
    Guid? ReleaseGroupId,
    string? Slug = null,
    string? Description = null,
    string? Upc = null,
    string? PrimaryGenre = null,
    string? Tags = null,
    string? LanguageCode = null,
    string? LabelName = null,
    string? PLine = null,
    string? CLine = null,
    DateTimeOffset? OriginalReleaseDate = null,
    bool MetadataComplete = false,
    IReadOnlyList<Guid>? CollaboratorArtistIds = null);

public sealed record UpdateReleaseRequest(
    string Title,
    ReleaseType ReleaseType,
    DateTimeOffset ReleaseDate,
    Guid? ReleaseGroupId,
    string? Slug = null,
    string? Description = null,
    string? Upc = null,
    string? PrimaryGenre = null,
    string? Tags = null,
    string? LanguageCode = null,
    string? LabelName = null,
    string? PLine = null,
    string? CLine = null,
    DateTimeOffset? OriginalReleaseDate = null,
    bool MetadataComplete = false,
    IReadOnlyList<Guid>? CollaboratorArtistIds = null);

public sealed record ManageReleaseSummaryResponse(
    Guid Id,
    string Slug,
    string Title,
    Guid ArtistId,
    string ArtistName,
    ReleaseType ReleaseType,
    ReleaseLifecycleStatus LifecycleStatus,
    DateTimeOffset ReleaseDate,
    Guid? ReleaseGroupId,
    string? ReleaseGroupTitle,
    string? ReleaseGroupSlug,
    string? Description,
    string? Upc,
    string? PrimaryGenre,
    string? Tags,
    string? LanguageCode,
    string? LabelName,
    string? PLine,
    string? CLine,
    DateTimeOffset? OriginalReleaseDate,
    bool MetadataComplete,
    string? CoverArtUrl,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record ManageReleaseListResponse(
    IReadOnlyList<ManageReleaseSummaryResponse> Items);

public sealed record ManageTrackResponse(
    Guid Id,
    string Title,
    int TrackNumber,
    int DurationMs,
    bool ExplicitFlag,
    string? Isrc,
    string? Lyrics,
    string? LanguageCode,
    string? VersionTitle,
    string? ComposerCredits,
    TrackLifecycleStatus LifecycleStatus,
    bool HasAudioMaster,
    bool HasAudioStream);

public sealed record ManageReleaseDetailResponse(
    Guid Id,
    string Slug,
    string Title,
    Guid ArtistId,
    string ArtistName,
    ReleaseType ReleaseType,
    ReleaseLifecycleStatus LifecycleStatus,
    DateTimeOffset ReleaseDate,
    Guid? ReleaseGroupId,
    string? ReleaseGroupTitle,
    string? ReleaseGroupSlug,
    string? Description,
    string? Upc,
    string? PrimaryGenre,
    string? Tags,
    string? LanguageCode,
    string? LabelName,
    string? PLine,
    string? CLine,
    DateTimeOffset? OriginalReleaseDate,
    bool MetadataComplete,
    string? CoverArtUrl,
    DateTimeOffset? PublishedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<ManageReleaseCollaboratorResponse> Collaborators,
    IReadOnlyList<ManageTrackResponse> Tracks);

public sealed record CreateTrackRequest(
    string Title,
    int TrackNumber,
    int DurationMs,
    bool ExplicitFlag,
    string? Isrc = null,
    string? Lyrics = null,
    string? LanguageCode = null,
    string? VersionTitle = null,
    string? ComposerCredits = null);

public sealed record UpdateTrackRequest(
    string Title,
    int TrackNumber,
    bool ExplicitFlag,
    string? Isrc = null,
    string? Lyrics = null,
    string? LanguageCode = null,
    string? VersionTitle = null,
    string? ComposerCredits = null);

public sealed record TrackIngestionResponse(
    Guid TrackId,
    TrackLifecycleStatus LifecycleStatus,
    string? AudioMasterKey,
    string? AudioStreamKey,
    Guid? LatestJobId,
    AudioTranscodeJobStatus? JobStatus,
    string? JobLastError,
    DateTimeOffset? JobUpdatedAt);

public sealed record PresignReleaseCoverUploadRequest(
    string FileName,
    string ContentType);

public sealed record PresignReleaseCoverUploadResponse(
    Guid ReleaseId,
    string Key,
    string Url,
    DateTimeOffset ExpiresAt,
    string Method);

public sealed record CompleteReleaseCoverUploadRequest(string Key);

public sealed record CompleteReleaseCoverUploadResponse(
    Guid ReleaseId,
    string CoverArtKey,
    string? CoverArtUrl);

public sealed record PresignArtistAvatarUploadRequest(
    string FileName,
    string ContentType);

public sealed record PresignArtistAvatarUploadResponse(
    Guid ArtistId,
    string Key,
    string Url,
    DateTimeOffset ExpiresAt,
    string Method);

public sealed record CompleteArtistAvatarUploadRequest(string Key);

public sealed record CompleteArtistAvatarUploadResponse(
    Guid ArtistId,
    string AvatarKey,
    string? AvatarUrl);

public sealed record PresignArtistCoverUploadRequest(
    string FileName,
    string ContentType);

public sealed record PresignArtistCoverUploadResponse(
    Guid ArtistId,
    string Key,
    string Url,
    DateTimeOffset ExpiresAt,
    string Method);

public sealed record CompleteArtistCoverUploadRequest(string Key);

public sealed record CompleteArtistCoverUploadResponse(
    Guid ArtistId,
    string CoverKey,
    string? CoverUrl);
