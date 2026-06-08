using Amuse.Domain.Catalog;
using Amuse.Domain.Billing;
using Amuse.Domain.Identity;
using Amuse.Modules.Billing.Contracts;
using Amuse.Modules.Billing.Persistence;
using Amuse.Modules.Catalog.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Billing.Services;

internal sealed class EntitlementReadModel(
    BillingDbContext db,
    ICatalogPurchaseReadModel catalog) : IEntitlementReadModel
{
    public async Task<bool> OwnsTrackAsync(
        AccountId accountId,
        Guid trackId,
        Guid releaseId,
        CancellationToken cancellationToken)
    {
        var purchases = await GetActivePurchasesForAccountAsync(accountId, cancellationToken);
        return EntitlementQuery.OwnsTrack(purchases, trackId, releaseId);
    }

    public async Task<bool> OwnsReleaseAsync(
        AccountId accountId,
        Guid releaseId,
        CancellationToken cancellationToken)
    {
        var purchases = await GetActivePurchasesForAccountAsync(accountId, cancellationToken);
        var sellableTrackIds = await catalog.GetSellablePublishedTrackIdsForReleaseAsync(
            ReleaseId.From(releaseId),
            cancellationToken);

        return EntitlementQuery.OwnsRelease(purchases, releaseId, sellableTrackIds);
    }

    public async Task<IReadOnlyList<Purchase>> GetActivePurchasesForAccountAsync(
        AccountId accountId,
        CancellationToken cancellationToken) =>
        await db.Purchases.AsNoTracking()
            .Where(p => p.AccountId == accountId && p.EntitlementStatus == EntitlementStatus.Active)
            .ToListAsync(cancellationToken);
}
