using Amuse.Domain.Catalog;

namespace Amuse.Modules.Catalog.Contracts;

public enum CatalogSearchItemKind
{
    Artist = 1,
    Release = 2,
    Track = 3,
}

public sealed record CatalogSearchItem(
    CatalogSearchItemKind Kind,
    Guid Id,
    string Title,
    string? Subtitle,
    string? ArtistSlug,
    string? ReleaseSlug,
    Guid? ArtistId,
    Guid? ReleaseId,
    string? CoverArtKey,
    string TrustTier,
    bool IsVerified);

public sealed record CatalogSearchResult(
    IReadOnlyList<CatalogSearchItem> Verified,
    IReadOnlyList<CatalogSearchItem> Unverified);

public sealed record CatalogTrackPlayableRow(
    Guid TrackId,
    string Title,
    int TrackNumber,
    int DurationMs,
    bool HasAudio,
    Guid ReleaseId,
    string ReleaseTitle,
    string ArtistName,
    string ArtistSlug,
    string ReleaseSlug);

public sealed record CatalogTrackDownloadRow(
    Guid TrackId,
    Guid ReleaseId,
    string AudioMasterKey,
    string Title);

public interface ICatalogDiscoveryReadModel
{
    Task<bool> TrackExistsAndPlayableAsync(TrackId trackId, CancellationToken cancellationToken);

    Task<bool> ReleaseExistsAndPublishedAsync(ReleaseId releaseId, CancellationToken cancellationToken);

    Task<IReadOnlyList<TrackId>> GetPublishedTrackIdsForReleaseOrderedAsync(
        ReleaseId releaseId,
        CancellationToken cancellationToken);

    Task<CatalogSearchResult> SearchAsync(string query, int limit, CancellationToken cancellationToken);

    Task<IReadOnlyList<CatalogTrackPlayableRow>> GetPlayableTracksForReleaseAsync(
        ReleaseId releaseId,
        CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<Guid, CatalogTrackPlayableRow>> GetPlayableTrackRowsAsync(
        IEnumerable<Guid> trackIds,
        CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<Guid, CatalogReleaseSummaryRow>> GetReleaseSummariesAsync(
        IEnumerable<Guid> releaseIds,
        CancellationToken cancellationToken);

    Task<CatalogTrackDownloadRow?> GetTrackDownloadRowAsync(
        TrackId trackId,
        CancellationToken cancellationToken);
}

public sealed record CatalogReleaseSummaryRow(
    Guid ReleaseId,
    string Title,
    string ArtistName,
    string ArtistSlug,
    string ReleaseSlug,
    string? CoverArtKey);
