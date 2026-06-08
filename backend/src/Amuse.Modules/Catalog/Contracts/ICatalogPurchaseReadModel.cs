using Amuse.Domain.Catalog;
using Amuse.Domain.Tenancy;

namespace Amuse.Modules.Catalog.Contracts;

public sealed record CatalogSellableTrackRow(
    Guid TrackId,
    Guid ReleaseId,
    OrganizationId OrganizationId,
    long PriceFloorMinor,
    long? PriceCeilingMinor,
    string? PriceCurrency,
    bool IsForSale);

public sealed record CatalogSellableReleaseRow(
    Guid ReleaseId,
    OrganizationId OrganizationId,
    long PriceFloorMinor,
    long? PriceCeilingMinor,
    string? PriceCurrency,
    bool IsForSale);

public sealed record CatalogCheckoutTrackRow(
    Guid TrackId,
    Guid ReleaseId,
    OrganizationId OrganizationId,
    string Title,
    long PriceFloorMinor,
    long? PriceCeilingMinor,
    string Currency,
    IReadOnlyList<CatalogRoyaltySplitRow> RoyaltySplits);

public sealed record CatalogCheckoutReleaseTrackRow(
    Guid TrackId,
    OrganizationId ListingOrganizationId,
    long PriceFloorMinor,
    IReadOnlyList<CatalogRoyaltySplitRow> RoyaltySplits);

public sealed record CatalogCheckoutReleaseRow(
    Guid ReleaseId,
    OrganizationId OrganizationId,
    string Title,
    long PriceFloorMinor,
    long? PriceCeilingMinor,
    string Currency,
    IReadOnlyList<CatalogCheckoutReleaseTrackRow> Tracks);

public sealed record CatalogRoyaltySplitRow(
    Guid TrackId,
    OrganizationId PayeeOrganizationId,
    int ShareBps);

public interface ICatalogPurchaseReadModel
{
    Task<CatalogSellableTrackRow?> GetSellableTrackAsync(TrackId trackId, CancellationToken cancellationToken);

    Task<CatalogSellableReleaseRow?> GetSellableReleaseAsync(
        ReleaseId releaseId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Guid>> GetSellablePublishedTrackIdsForReleaseAsync(
        ReleaseId releaseId,
        CancellationToken cancellationToken);

    Task<CatalogCheckoutTrackRow?> GetCheckoutTrackAsync(TrackId trackId, CancellationToken cancellationToken);

    Task<CatalogCheckoutReleaseRow?> GetCheckoutReleaseAsync(
        ReleaseId releaseId,
        CancellationToken cancellationToken);
}
