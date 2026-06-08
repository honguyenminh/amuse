using Amuse.Domain.Catalog;

namespace Amuse.Modules.Catalog.Features.Common;

public sealed record ArtistSummary(
    Guid Id,
    string Slug,
    string Name,
    string? AvatarUrl,
    string? CoverUrl,
    string TrustTier);

public sealed record ReleaseSummary(
    Guid Id,
    string Slug,
    string Title,
    Guid ArtistId,
    string ArtistName,
    string ArtistSlug,
    ReleaseType ReleaseType,
    DateTimeOffset ReleaseDate,
    string? CoverArtUrl,
    string TrustTier);

public sealed record TrackResponse(
    Guid Id,
    string Title,
    int TrackNumber,
    int DurationMs,
    bool HasAudio,
    CatalogPricingResponse? Pricing = null);
