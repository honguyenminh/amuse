using Amuse.Domain.Identity;
using Amuse.Domain.SharedKernel;
using Amuse.Domain.Tenancy;

namespace Amuse.Domain.Billing;

public static class ReleaseEntitlementCompletion
{
    public static Result<Purchase?> TryGrantOnCompleteTrackSet(
        AccountId accountId,
        OrganizationId organizationId,
        Guid releaseId,
        IReadOnlyList<Guid> sellableTrackIds,
        IReadOnlyList<Purchase> existingPurchases,
        Money zeroPriceSnapshot,
        DateTimeOffset now)
    {
        if (EntitlementQuery.HasActiveReleaseEntitlement(existingPurchases, releaseId))
            return Result<Purchase?>.Success(null);

        if (!EntitlementQuery.IsCompleteTrackSet(existingPurchases, sellableTrackIds))
            return Result<Purchase?>.Success(null);

        var releasePurchase = Purchase.AcquireFreeRelease(
            accountId,
            organizationId,
            releaseId,
            zeroPriceSnapshot,
            now);

        if (!releasePurchase.IsSuccess)
            return Result<Purchase?>.Failure(releasePurchase.Error!);

        return Result<Purchase?>.Success(releasePurchase.Value);
    }

    public static Result<Purchase?> TryGrantPaidOnCompleteTrackSet(
        AccountId accountId,
        OrganizationId organizationId,
        Guid releaseId,
        IReadOnlyList<Guid> sellableTrackIds,
        IReadOnlyList<Purchase> existingPurchases,
        Money zeroPriceSnapshot,
        DateTimeOffset now)
    {
        if (EntitlementQuery.HasActiveReleaseEntitlement(existingPurchases, releaseId))
            return Result<Purchase?>.Success(null);

        if (!EntitlementQuery.IsCompleteTrackSet(existingPurchases, sellableTrackIds))
            return Result<Purchase?>.Success(null);

        var releasePurchase = Purchase.AcquireFreeRelease(
            accountId,
            organizationId,
            releaseId,
            zeroPriceSnapshot,
            now);

        if (!releasePurchase.IsSuccess)
            return Result<Purchase?>.Failure(releasePurchase.Error!);

        return Result<Purchase?>.Success(releasePurchase.Value);
    }
}
