using Amuse.Domain.SharedKernel;

namespace Amuse.Domain.Catalog;

public static class CatalogPublishForSaleValidation
{
    public static Result ValidateReleasePricingAgainstTracks(Release release)
    {
        if (!release.IsForSale)
            return Result.Success();

        if (release.Tracks.Count == 0)
            return Result.Failure(CatalogErrors.ReleaseHasNoTracks);

        var trackFloorsSum = 0L;
        long trackCeilingsSum = 0L;
        var allTracksHaveCeilings = true;

        foreach (var track in release.Tracks)
        {
            trackFloorsSum += track.PriceFloorMinor;

            if (track.PriceCeilingMinor.HasValue)
                trackCeilingsSum += track.PriceCeilingMinor.Value;
            else
                allTracksHaveCeilings = false;
        }

        if (release.PriceFloorMinor > trackFloorsSum)
            return Result.Failure(CatalogErrors.ReleaseFloorExceedsTrackFloors);

        if (release.PriceCeilingMinor.HasValue
            && allTracksHaveCeilings
            && release.PriceCeilingMinor.Value > trackCeilingsSum)
        {
            return Result.Failure(CatalogErrors.ReleaseCeilingExceedsTrackCeilings);
        }

        return Result.Success();
    }

    public static Result ValidateTrackPricing(Track track)
    {
        if (!track.IsForSale)
            return Result.Success();

        var pricingResult = CatalogPricing.TryCreate(
            track.IsForSale,
            track.PriceFloorMinor,
            track.PriceCeilingMinor,
            track.PriceCurrency);

        return pricingResult.IsSuccess
            ? Result.Success()
            : Result.Failure(pricingResult.Error!);
    }

    public static Result ValidateRoyaltySplitsForTrack(
        Track track,
        IReadOnlyList<RoyaltySplit> splits)
    {
        if (!track.IsForSale)
            return Result.Success();

        return RoyaltySplit.ValidateForPublish(track.Id, splits);
    }
}
