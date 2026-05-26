using Amuse.Domain.Catalog;

namespace Amuse.Modules.Catalog.Features.Shared;

public sealed record ArtistSummary(
    Guid Id,
    string Slug,
    string Name,
    string? AvatarUrl,
    string? CoverUrl);

public sealed record AlbumSummary(
    Guid Id,
    string Slug,
    string Title,
    Guid ArtistId,
    string ArtistName,
    string ArtistSlug,
    ReleaseType ReleaseType,
    DateTimeOffset ReleaseDate,
    string? CoverArtUrl);

public sealed record TrackResponse(
    Guid Id,
    string Title,
    int TrackNumber,
    int DurationMs,
    bool HasAudio);
