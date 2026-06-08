using System.Security.Claims;
using Amuse.Domain.Billing;
using Amuse.Domain.Catalog;
using Amuse.Domain.Identity;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Billing.Features.Common;
using Amuse.Modules.Billing.Persistence;
using Amuse.Modules.Catalog.Contracts;
using Amuse.Modules.Common.Time;
using Amuse.Modules.Discovery.Features.Common;
using Amuse.Modules.Identity.Contracts;
using Amuse.Modules.Identity.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Billing.Features.AcquireFree;

internal sealed class AcquireFreeHandler(
    BillingDbContext billingDb,
    IdentityDbContext identityDb,
    ICatalogPurchaseReadModel catalog,
    IListenerPersonaReadModel personaReadModel,
    IClock clock)
{
    public async Task<Result<FreeAcquisitionResponse>> HandleAsync(
        FreeAcquisitionRequest request,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var listenerResult = await DiscoveryPrincipal.RequireListenerAsync(
            principal, personaReadModel, cancellationToken);
        if (!listenerResult.IsSuccess)
            return Result<FreeAcquisitionResponse>.Failure(listenerResult.Error!);

        var accountId = listenerResult.Value!.AccountId;

        var banned = await identityDb.Accounts.AsNoTracking()
            .AnyAsync(a => a.Id == accountId && a.Status == AccountStatus.Banned, cancellationToken);
        if (banned)
            return Result<FreeAcquisitionResponse>.Failure(BillingErrors.AccountBanned);

        if (request.TrackId is { } trackId)
            return await AcquireTrackAsync(accountId, trackId, cancellationToken);

        if (request.ReleaseId is { } releaseId)
            return await AcquireReleaseAsync(accountId, releaseId, cancellationToken);

        return Result<FreeAcquisitionResponse>.Failure(BillingErrors.InvalidAcquisitionTarget);
    }

    private async Task<Result<FreeAcquisitionResponse>> AcquireTrackAsync(
        AccountId accountId,
        Guid trackId,
        CancellationToken cancellationToken)
    {
        var catalogTrack = await catalog.GetSellableTrackAsync(TrackId.From(trackId), cancellationToken);
        if (catalogTrack is null)
            return Result<FreeAcquisitionResponse>.Failure(BillingErrors.TrackNotFound);

        if (!catalogTrack.IsForSale || catalogTrack.PriceFloorMinor != 0)
            return Result<FreeAcquisitionResponse>.Failure(BillingErrors.NotFreeEligible);

        var duplicate = await billingDb.Purchases.AnyAsync(
            p => p.AccountId == accountId
                 && p.TrackId == trackId
                 && p.EntitlementStatus == EntitlementStatus.Active,
            cancellationToken);
        if (duplicate)
            return Result<FreeAcquisitionResponse>.Failure(BillingErrors.PurchaseDuplicate);

        var zeroMoney = CreateZeroMoney(catalogTrack.PriceCurrency);
        if (!zeroMoney.IsSuccess)
            return Result<FreeAcquisitionResponse>.Failure(zeroMoney.Error!);

        var now = clock.UtcNow;
        var purchase = Purchase.AcquireFreeTrack(
            accountId,
            catalogTrack.OrganizationId,
            trackId,
            zeroMoney.Value!,
            now);
        if (!purchase.IsSuccess)
            return Result<FreeAcquisitionResponse>.Failure(purchase.Error!);

        billingDb.Purchases.Add(purchase.Value!);

        var releaseEntitlementGranted = false;
        var sellableTrackIds = await catalog.GetSellablePublishedTrackIdsForReleaseAsync(
            ReleaseId.From(catalogTrack.ReleaseId),
            cancellationToken);

        var existingPurchases = await billingDb.Purchases
            .Where(p => p.AccountId == accountId && p.EntitlementStatus == EntitlementStatus.Active)
            .ToListAsync(cancellationToken);
        existingPurchases.Add(purchase.Value!);

        var completion = ReleaseEntitlementCompletion.TryGrantOnCompleteTrackSet(
            accountId,
            catalogTrack.OrganizationId,
            catalogTrack.ReleaseId,
            sellableTrackIds,
            existingPurchases,
            zeroMoney.Value!,
            now);

        if (!completion.IsSuccess)
            return Result<FreeAcquisitionResponse>.Failure(completion.Error!);

        if (completion.Value is { } releasePurchase)
        {
            billingDb.Purchases.Add(releasePurchase);
            releaseEntitlementGranted = true;
        }

        await billingDb.SaveChangesAsync(cancellationToken);

        return Result<FreeAcquisitionResponse>.Success(new FreeAcquisitionResponse(
            purchase.Value!.Id.Value,
            PurchasedUnit.Track.ToString().ToLowerInvariant(),
            trackId,
            null,
            releaseEntitlementGranted));
    }

    private async Task<Result<FreeAcquisitionResponse>> AcquireReleaseAsync(
        AccountId accountId,
        Guid releaseId,
        CancellationToken cancellationToken)
    {
        var catalogRelease = await catalog.GetSellableReleaseAsync(ReleaseId.From(releaseId), cancellationToken);
        if (catalogRelease is null)
            return Result<FreeAcquisitionResponse>.Failure(BillingErrors.ReleaseNotFound);

        if (!catalogRelease.IsForSale || catalogRelease.PriceFloorMinor != 0)
            return Result<FreeAcquisitionResponse>.Failure(BillingErrors.NotFreeEligible);

        var duplicate = await billingDb.Purchases.AnyAsync(
            p => p.AccountId == accountId
                 && p.ReleaseId == releaseId
                 && p.PurchasedUnit == PurchasedUnit.Release
                 && p.EntitlementStatus == EntitlementStatus.Active,
            cancellationToken);
        if (duplicate)
            return Result<FreeAcquisitionResponse>.Failure(BillingErrors.PurchaseReleaseDuplicate);

        var zeroMoney = CreateZeroMoney(catalogRelease.PriceCurrency);
        if (!zeroMoney.IsSuccess)
            return Result<FreeAcquisitionResponse>.Failure(zeroMoney.Error!);

        var purchase = Purchase.AcquireFreeRelease(
            accountId,
            catalogRelease.OrganizationId,
            releaseId,
            zeroMoney.Value!,
            clock.UtcNow);
        if (!purchase.IsSuccess)
            return Result<FreeAcquisitionResponse>.Failure(purchase.Error!);

        billingDb.Purchases.Add(purchase.Value!);
        await billingDb.SaveChangesAsync(cancellationToken);

        return Result<FreeAcquisitionResponse>.Success(new FreeAcquisitionResponse(
            purchase.Value!.Id.Value,
            PurchasedUnit.Release.ToString().ToLowerInvariant(),
            null,
            releaseId,
            ReleaseEntitlementGranted: true));
    }

    private static Result<Money> CreateZeroMoney(string? currency) =>
        Money.Create(0, string.IsNullOrWhiteSpace(currency) ? "USD" : currency);
}
