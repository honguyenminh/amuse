using Amuse.Domain.Billing;
using Amuse.Domain.Tenancy;

namespace Amuse.Domain.Tests.Billing;

public sealed class PurchaseWaterfallTests
{
    [Fact]
    public void Compute_extracts_inclusive_vat_and_platform_fee_from_gross()
    {
        var result = PurchaseWaterfall.Compute(
            grossMinor: 1100,
            currency: "USD",
            pspFeeMinor: 33,
            vatRateBps: 1000,
            platformFeeRateBps: 1000);

        Assert.True(result.IsSuccess);
        var waterfall = result.Value!;
        Assert.Equal(100, waterfall.VatMinor);
        Assert.Equal(1000, waterfall.NetExVatMinor);
        Assert.Equal(110, waterfall.PlatformFeeMinor);
        Assert.Equal(857, waterfall.NetToSellersMinor);
    }

    [Fact]
    public void Compute_rejects_when_psp_fee_exceeds_seller_pool()
    {
        var result = PurchaseWaterfall.Compute(
            grossMinor: 500,
            currency: "USD",
            pspFeeMinor: 450,
            vatRateBps: 1000,
            platformFeeRateBps: 1000);

        Assert.False(result.IsSuccess);
        Assert.Equal(BillingErrors.InvalidLedgerJournal, result.Error);
    }

    [Fact]
    public void ExtractInclusiveVat_rounds_away_from_zero()
    {
        Assert.Equal(91, PurchaseWaterfall.ExtractInclusiveVat(1000, 1000));
    }
}

public sealed class PurchaseAllocationTests
{
    [Fact]
    public void AllocateTrack_splits_net_by_royalty_shares_with_remainder()
    {
        var listingOrg = OrganizationId.New();
        var collabOrg = OrganizationId.New();
        var trackId = Guid.CreateVersion7();

        var result = PurchaseAllocation.AllocateTrack(
            trackId,
            listingOrg,
            netToSellersMinor: 100,
            [
                new RoyaltySplitSnapshot(trackId, listingOrg, 6000),
                new RoyaltySplitSnapshot(trackId, collabOrg, 4000),
            ]);

        Assert.True(result.IsSuccess);
        var lines = result.Value!;
        Assert.Equal(100, lines.Sum(l => l.AmountMinor));
        Assert.Equal(60, lines.Single(l => l.PayeeOrganizationId == listingOrg).AmountMinor);
        Assert.Equal(40, lines.Single(l => l.PayeeOrganizationId == collabOrg).AmountMinor);
    }

    [Fact]
    public void AllocateRelease_uses_track_floor_weights_then_splits()
    {
        var listingOrg = OrganizationId.New();
        var trackA = Guid.CreateVersion7();
        var trackB = Guid.CreateVersion7();

        var result = PurchaseAllocation.AllocateRelease(
            netToSellersMinor: 1000,
            [
                new ReleaseTrackAllocationInput(trackA, listingOrg, 300, []),
                new ReleaseTrackAllocationInput(trackB, listingOrg, 700, []),
            ]);

        Assert.True(result.IsSuccess);
        var lines = result.Value!;
        Assert.Equal(1000, lines.Sum(l => l.AmountMinor));
        Assert.Equal(300, lines.Where(l => l.TrackId == trackA).Sum(l => l.AmountMinor));
        Assert.Equal(700, lines.Where(l => l.TrackId == trackB).Sum(l => l.AmountMinor));
    }

    [Fact]
    public void AllocateRelease_uses_equal_split_when_all_track_floors_zero()
    {
        var listingOrg = OrganizationId.New();
        var trackA = Guid.CreateVersion7();
        var trackB = Guid.CreateVersion7();

        var result = PurchaseAllocation.AllocateRelease(
            netToSellersMinor: 101,
            [
                new ReleaseTrackAllocationInput(trackA, listingOrg, 0, []),
                new ReleaseTrackAllocationInput(trackB, listingOrg, 0, []),
            ]);

        Assert.True(result.IsSuccess);
        var lines = result.Value!;
        Assert.Equal(101, lines.Sum(l => l.AmountMinor));
        Assert.Equal(51, lines.Where(l => l.TrackId == trackA).Sum(l => l.AmountMinor));
        Assert.Equal(50, lines.Where(l => l.TrackId == trackB).Sum(l => l.AmountMinor));
    }
}

public sealed class JournalPosterTests
{
    [Fact]
    public void PostPurchase_creates_balanced_journal_and_snapshots()
    {
        var purchaseId = PurchaseId.New();
        var org = OrganizationId.New();
        var trackId = Guid.CreateVersion7();
        var paidAt = DateTimeOffset.Parse("2026-06-08T12:00:00+00:00");

        var waterfall = new PurchaseWaterfallResult(
            GrossMinor: 1100,
            VatMinor: 100,
            NetExVatMinor: 1000,
            PlatformFeeMinor: 110,
            PspFeeMinor: 33,
            NetToSellersMinor: 857,
            VatRateBps: 1000,
            PlatformFeeRateBps: 1000,
            Currency: "USD");

        var allocation = new[]
        {
            new AllocationPayeeLine(trackId, org, 10_000, 857),
        };

        var result = JournalPoster.PostPurchase(
            purchaseId,
            waterfall,
            allocation,
            paidAt,
            holdDays: 3);

        Assert.True(result.IsSuccess);
        var journal = result.Value!.Journal;
        var debits = journal.Entries.Where(e => e.Direction == EntryDirection.Debit).Sum(e => e.AmountMinor);
        var credits = journal.Entries.Where(e => e.Direction == EntryDirection.Credit).Sum(e => e.AmountMinor);
        Assert.Equal(debits, credits);
        Assert.Equal(paidAt.AddDays(3), journal.AvailableAt);
        Assert.Single(result.Value!.Snapshots);
    }
}

public sealed class CheckoutPricingGuardTests
{
    [Fact]
    public void ValidateAmount_rejects_below_floor_and_above_ceiling()
    {
        Assert.False(CheckoutPricingGuard.ValidateAmount(99, 100, 500).IsSuccess);
        Assert.False(CheckoutPricingGuard.ValidateAmount(501, 100, 500).IsSuccess);
        Assert.True(CheckoutPricingGuard.ValidateAmount(250, 100, 500).IsSuccess);
    }
}
