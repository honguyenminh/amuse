using Amuse.Domain.Catalog;

namespace Amuse.Modules.Catalog.Features.Common;

internal static class CatalogPricingMapper
{
    internal static CatalogPricingResponse ToResponse(CatalogPricing pricing) =>
        new(
            pricing.IsForSale,
            pricing.PriceFloorMinor,
            pricing.PriceCeilingMinor,
            pricing.PriceCurrency);

    internal static CatalogPricingResponse ToResponse(Track track) =>
        ToResponse(
            CatalogPricing.TryCreate(
                track.IsForSale,
                track.PriceFloorMinor,
                track.PriceCeilingMinor,
                track.PriceCurrency).Value!);

    internal static CatalogPricingResponse ToResponse(Release release) =>
        ToResponse(
            CatalogPricing.TryCreate(
                release.IsForSale,
                release.PriceFloorMinor,
                release.PriceCeilingMinor,
                release.PriceCurrency).Value!);

    internal static CatalogPricingResponse? ToPublicResponse(Track track) =>
        track.IsForSale ? ToResponse(track) : null;

    internal static CatalogPricingResponse? ToPublicResponse(Release release) =>
        release.IsForSale ? ToResponse(release) : null;

    internal static IReadOnlyList<RoyaltySplitResponse> ToRoyaltySplitResponses(
        IReadOnlyList<RoyaltySplit> splits) =>
        splits
            .OrderByDescending(split => split.ShareBps)
            .Select(split => new RoyaltySplitResponse(
                split.PayeeOrganizationId.Value,
                split.ShareBps))
            .ToArray();
}
