namespace Amuse.Domain.Billing;

public static class EntitlementQuery
{
    public static bool HasActiveTrackEntitlement(IEnumerable<Purchase> purchases, Guid trackId) =>
        purchases.Any(p =>
            p.HasActiveEntitlement
            && p.PurchasedUnit == PurchasedUnit.Track
            && p.TrackId == trackId);

    public static bool HasActiveReleaseEntitlement(IEnumerable<Purchase> purchases, Guid releaseId) =>
        purchases.Any(p =>
            p.HasActiveEntitlement
            && p.PurchasedUnit == PurchasedUnit.Release
            && p.ReleaseId == releaseId);

    public static bool OwnsTrack(
        IEnumerable<Purchase> purchases,
        Guid trackId,
        Guid releaseId) =>
        HasActiveTrackEntitlement(purchases, trackId)
        || HasActiveReleaseEntitlement(purchases, releaseId);

    public static bool OwnsRelease(
        IEnumerable<Purchase> purchases,
        Guid releaseId,
        IReadOnlyList<Guid> sellableTrackIds)
    {
        if (HasActiveReleaseEntitlement(purchases, releaseId))
            return true;

        if (sellableTrackIds.Count == 0)
            return false;

        return sellableTrackIds.All(trackId => HasActiveTrackEntitlement(purchases, trackId));
    }

    public static bool IsCompleteTrackSet(
        IEnumerable<Purchase> purchases,
        IReadOnlyList<Guid> sellableTrackIds) =>
        sellableTrackIds.Count > 0
        && sellableTrackIds.All(trackId => HasActiveTrackEntitlement(purchases, trackId));
}
