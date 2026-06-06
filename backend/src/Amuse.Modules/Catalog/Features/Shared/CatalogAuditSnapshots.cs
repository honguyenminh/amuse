using Amuse.Domain.Catalog;

namespace Amuse.Modules.Catalog.Features.Shared;

internal sealed record ArtistAuditSnapshot(
    Guid Id,
    string Name,
    string Slug,
    string? Bio,
    string? CountryCode,
    string? WebsiteUrl,
    string? Aliases,
    string? AvatarKey,
    string? CoverKey);

internal sealed record ReleaseAuditSnapshot(
    Guid Id,
    string Title,
    string Slug,
    ReleaseType ReleaseType,
    ReleaseLifecycleStatus LifecycleStatus,
    DateTimeOffset ReleaseDate,
    Guid? ReleaseGroupId,
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
    string? CoverArtKey);

internal sealed record TrackAuditSnapshot(
    Guid Id,
    Guid ReleaseId,
    string Title,
    int TrackNumber,
    int DurationMs,
    bool ExplicitFlag,
    string? Isrc,
    string? Lyrics,
    string? LanguageCode,
    string? VersionTitle,
    string? ComposerCredits,
    TrackLifecycleStatus LifecycleStatus);

internal sealed record ReleaseGroupAuditSnapshot(
    Guid Id,
    Guid ArtistId,
    string Title,
    string Slug,
    string? Description);

internal static class CatalogAuditSnapshotMapper
{
    internal static ArtistAuditSnapshot FromArtist(Artist artist) =>
        new(
            artist.Id.Value,
            artist.Name,
            artist.Slug.Value,
            artist.Bio,
            artist.CountryCode,
            artist.WebsiteUrl,
            artist.Aliases,
            artist.AvatarKey,
            artist.CoverKey);

    internal static ReleaseAuditSnapshot FromRelease(Release release) =>
        new(
            release.Id.Value,
            release.Title,
            release.Slug.Value,
            release.ReleaseType,
            release.LifecycleStatus,
            release.ReleaseDate,
            release.ReleaseGroupId?.Value,
            release.Description,
            release.Upc,
            release.PrimaryGenre,
            release.Tags,
            release.LanguageCode,
            release.LabelName,
            release.PLine,
            release.CLine,
            release.OriginalReleaseDate,
            release.MetadataComplete,
            release.CoverArtKey);

    internal static TrackAuditSnapshot FromTrack(Track track) =>
        new(
            track.Id.Value,
            track.ReleaseId.Value,
            track.Title,
            track.TrackNumber,
            track.Duration.Milliseconds,
            track.ExplicitFlag,
            track.Isrc,
            track.Lyrics,
            track.LanguageCode,
            track.VersionTitle,
            track.ComposerCredits,
            track.LifecycleStatus);

    internal static ReleaseGroupAuditSnapshot FromReleaseGroup(ReleaseGroup group) =>
        new(
            group.Id.Value,
            group.ArtistId.Value,
            group.Title,
            group.Slug.Value,
            group.Description);
}
