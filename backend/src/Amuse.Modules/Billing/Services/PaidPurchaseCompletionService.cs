using Amuse.Domain.Billing;
using Amuse.Domain.Catalog;
using Amuse.Domain.Identity;
using Amuse.Domain.SharedKernel;
using Amuse.Domain.Tenancy;
using Amuse.Modules.Billing.Contracts;
using Amuse.Modules.Billing.Persistence;
using Amuse.Modules.Catalog.Contracts;
using Amuse.Modules.Common.Time;
using Amuse.Modules.Tenancy.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Amuse.Modules.Billing.Services;

internal sealed class PaidPurchaseCompletionService(
    BillingDbContext billingDb,
    ICatalogPurchaseReadModel catalog,
    ITenancyOrganizationReadModel tenancy,
    IOptions<PlatformFeeConfig> platformFeeOptions,
    IOptions<TaxConfig> taxOptions,
    IOptions<HoldConfig> holdOptions,
    IClock clock)
{
    public async Task<Result<Guid>> CompleteAsync(
        Purchase purchase,
        PaymentTransaction paymentTransaction,
        CompletedCheckoutPayment payment,
        CancellationToken cancellationToken)
    {
        if (purchase.PaymentStatus == PaymentStatus.Paid)
            return Result<Guid>.Success(purchase.Id.Value);

        if (purchase.PaymentStatus != PaymentStatus.Pending)
            return Result<Guid>.Failure(BillingErrors.InvalidPaymentStatusTransition);

        if (!string.IsNullOrWhiteSpace(payment.PaymentMethodFingerprint))
        {
            var fingerprintBanned = await billingDb.BannedPaymentInstruments.AsNoTracking()
                .AnyAsync(
                    instrument => instrument.PaymentMethodFingerprint == payment.PaymentMethodFingerprint.Trim(),
                    cancellationToken);

            if (fingerprintBanned)
                return Result<Guid>.Failure(BillingErrors.PaymentInstrumentBanned);
        }

        var grossMoney = Money.Create(payment.GrossMinor, payment.Currency);
        if (!grossMoney.IsSuccess)
            return Result<Guid>.Failure(grossMoney.Error!);

        if (paymentTransaction.GrossMinor != payment.GrossMinor
            || !string.Equals(paymentTransaction.Currency, payment.Currency, StringComparison.OrdinalIgnoreCase))
        {
            return Result<Guid>.Failure(BillingErrors.InvalidCheckoutAmount);
        }

        var platformFeeRateBps = platformFeeOptions.Value.DefaultRateBps;
        var vatRateBps = taxOptions.Value.DefaultVatBps;

        var waterfall = PurchaseWaterfall.Compute(
            payment.GrossMinor,
            payment.Currency,
            payment.PspFeeMinor,
            vatRateBps,
            platformFeeRateBps);

        if (!waterfall.IsSuccess)
            return Result<Guid>.Failure(waterfall.Error!);

        var allocationResult = await BuildAllocationAsync(purchase, waterfall.Value!, cancellationToken);
        if (!allocationResult.IsSuccess)
            return Result<Guid>.Failure(allocationResult.Error!);

        var paidAt = clock.UtcNow;
        var holdDays = Math.Max(0, holdOptions.Value.Days);

        var journalResult = JournalPoster.PostPurchase(
            purchase.Id,
            waterfall.Value!,
            allocationResult.Value!,
            paidAt,
            holdDays);

        if (!journalResult.IsSuccess)
            return Result<Guid>.Failure(journalResult.Error!);

        var completePayment = paymentTransaction.MarkCompleted(
            payment.ProviderReference,
            payment.PaymentMethodFingerprint,
            payment.PspFeeMinor,
            paidAt);

        if (!completePayment.IsSuccess)
            return Result<Guid>.Failure(completePayment.Error!);

        var markPaid = purchase.MarkPaid(paymentTransaction.Id, paidAt);
        if (!markPaid.IsSuccess)
            return Result<Guid>.Failure(markPaid.Error!);

        billingDb.LedgerJournals.Add(journalResult.Value!.Journal);
        billingDb.PurchaseAllocationSnapshots.AddRange(journalResult.Value!.Snapshots);

        var latestInvoice = await billingDb.TaxInvoices.AsNoTracking()
            .OrderByDescending(i => i.IssuedAt)
            .Select(i => i.InvoiceNumber)
            .FirstOrDefaultAsync(cancellationToken);

        var invoiceNumber = TaxInvoiceNumber.NextFromLatest(latestInvoice, paidAt);
        var taxInvoice = TaxInvoice.Issue(
            invoiceNumber,
            purchase.Id,
            purchase.AccountId,
            waterfall.Value!.GrossMinor,
            waterfall.Value!.VatMinor,
            waterfall.Value!.NetExVatMinor,
            waterfall.Value!.Currency,
            waterfall.Value!.VatRateBps,
            paidAt);

        billingDb.TaxInvoices.Add(taxInvoice);

        Purchase? releaseEntitlement = null;
        if (purchase.PurchasedUnit == PurchasedUnit.Track && purchase.TrackId is { } trackId)
        {
            var catalogTrack = await catalog.GetSellableTrackAsync(TrackId.From(trackId), cancellationToken);
            if (catalogTrack is not null)
            {
                var sellableTrackIds = await catalog.GetSellablePublishedTrackIdsForReleaseAsync(
                    ReleaseId.From(catalogTrack.ReleaseId),
                    cancellationToken);

                var existingPurchases = await billingDb.Purchases
                    .Where(p => p.AccountId == purchase.AccountId && p.EntitlementStatus == EntitlementStatus.Active)
                    .ToListAsync(cancellationToken);

                if (!existingPurchases.Any(p => p.Id == purchase.Id))
                    existingPurchases.Add(purchase);

                var zeroMoney = Money.Create(0, purchase.Currency);
                if (zeroMoney.IsSuccess)
                {
                    var completion = ReleaseEntitlementCompletion.TryGrantPaidOnCompleteTrackSet(
                        purchase.AccountId,
                        purchase.OrganizationId,
                        catalogTrack.ReleaseId,
                        sellableTrackIds,
                        existingPurchases,
                        zeroMoney.Value!,
                        paidAt);

                    if (!completion.IsSuccess)
                        return Result<Guid>.Failure(completion.Error!);

                    releaseEntitlement = completion.Value;
                    if (releaseEntitlement is not null)
                        billingDb.Purchases.Add(releaseEntitlement);
                }
            }
        }

        await billingDb.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(purchase.Id.Value);
    }

    private async Task<Result<IReadOnlyList<AllocationPayeeLine>>> BuildAllocationAsync(
        Purchase purchase,
        PurchaseWaterfallResult waterfall,
        CancellationToken cancellationToken)
    {
        if (purchase.PurchasedUnit == PurchasedUnit.Track && purchase.TrackId is { } trackId)
        {
            var checkoutTrack = await catalog.GetCheckoutTrackAsync(TrackId.From(trackId), cancellationToken);
            if (checkoutTrack is null)
                return Result<IReadOnlyList<AllocationPayeeLine>>.Failure(BillingErrors.TrackNotFound);

            var splits = checkoutTrack.RoyaltySplits
                .Select(s => new RoyaltySplitSnapshot(s.TrackId, s.PayeeOrganizationId, s.ShareBps))
                .ToArray();

            return PurchaseAllocation.AllocateTrack(
                trackId,
                checkoutTrack.OrganizationId,
                waterfall.NetToSellersMinor,
                splits);
        }

        if (purchase.PurchasedUnit == PurchasedUnit.Release && purchase.ReleaseId is { } releaseId)
        {
            var checkoutRelease = await catalog.GetCheckoutReleaseAsync(ReleaseId.From(releaseId), cancellationToken);
            if (checkoutRelease is null)
                return Result<IReadOnlyList<AllocationPayeeLine>>.Failure(BillingErrors.ReleaseNotFound);

            var lifecycle = await tenancy.GetLifecycleStatusAsync(checkoutRelease.OrganizationId, cancellationToken);
            if (lifecycle is OrganizationLifecycleStatus.Suspended or OrganizationLifecycleStatus.Closed)
                return Result<IReadOnlyList<AllocationPayeeLine>>.Failure(BillingErrors.OrgSalesBlocked);

            var tracks = checkoutRelease.Tracks
                .Select(track => new ReleaseTrackAllocationInput(
                    track.TrackId,
                    track.ListingOrganizationId,
                    track.PriceFloorMinor,
                    track.RoyaltySplits
                        .Select(s => new RoyaltySplitSnapshot(s.TrackId, s.PayeeOrganizationId, s.ShareBps))
                        .ToArray()))
                .ToArray();

            return PurchaseAllocation.AllocateRelease(waterfall.NetToSellersMinor, tracks);
        }

        return Result<IReadOnlyList<AllocationPayeeLine>>.Failure(BillingErrors.InvalidAcquisitionTarget);
    }
}
