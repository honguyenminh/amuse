using Amuse.Domain.Billing;
using Amuse.Domain.Tenancy;

namespace Amuse.Domain.Tests.Billing;

public sealed class RefundClawbackTests
{
    [Fact]
    public void Compute_debits_pending_before_available()
    {
        var org = OrganizationId.New();
        var purchaseId = PurchaseId.New();
        var trackId = Guid.CreateVersion7();
        var paidAt = DateTimeOffset.Parse("2026-06-08T12:00:00+00:00");

        var snapshots = new[]
        {
            PurchaseAllocationSnapshot.Create(purchaseId, trackId, org, 10_000, 500, "USD", paidAt),
        };

        var journalId = LedgerJournalId.New();
        var entries = new[]
        {
            LedgerEntry.Create(journalId, LedgerAccountType.SellerPayablePending, org.Value, EntryDirection.Credit, 300, "USD"),
            LedgerEntry.Create(journalId, LedgerAccountType.SellerPayableAvailable, org.Value, EntryDirection.Credit, 200, "USD"),
            LedgerEntry.Create(journalId, LedgerAccountType.PlatformCash, null, EntryDirection.Debit, 500, "USD"),
        };

        var result = RefundClawback.Compute(snapshots, entries, refundFeeMinor: 0, RefundFeeBearer.Seller, "USD");

        Assert.Equal(300, result.LedgerLines.Single(line => line.AccountType == LedgerAccountType.SellerPayablePending).AmountMinor);
        Assert.Equal(200, result.LedgerLines.Single(line => line.AccountType == LedgerAccountType.SellerPayableAvailable).AmountMinor);
        Assert.Empty(result.Receivables);
    }

    [Fact]
    public void Compute_creates_receivable_when_balance_insufficient()
    {
        var org = OrganizationId.New();
        var purchaseId = PurchaseId.New();
        var trackId = Guid.CreateVersion7();
        var paidAt = DateTimeOffset.Parse("2026-06-08T12:00:00+00:00");

        var snapshots = new[]
        {
            PurchaseAllocationSnapshot.Create(purchaseId, trackId, org, 10_000, 500, "USD", paidAt),
        };

        var journalId = LedgerJournalId.New();
        var entries = new[]
        {
            LedgerEntry.Create(journalId, LedgerAccountType.SellerPayableAvailable, org.Value, EntryDirection.Credit, 100, "USD"),
            LedgerEntry.Create(journalId, LedgerAccountType.PlatformCash, null, EntryDirection.Debit, 100, "USD"),
        };

        var result = RefundClawback.Compute(snapshots, entries, refundFeeMinor: 0, RefundFeeBearer.Seller, "USD");

        Assert.Single(result.Receivables);
        Assert.Equal(400, result.Receivables[0].AmountMinor);
    }

    [Fact]
    public void Compute_distributes_refund_fee_to_sellers_pro_rata()
    {
        var orgA = OrganizationId.New();
        var orgB = OrganizationId.New();
        var purchaseId = PurchaseId.New();
        var paidAt = DateTimeOffset.Parse("2026-06-08T12:00:00+00:00");

        var snapshots = new[]
        {
            PurchaseAllocationSnapshot.Create(purchaseId, Guid.CreateVersion7(), orgA, 6000, 600, "USD", paidAt),
            PurchaseAllocationSnapshot.Create(purchaseId, Guid.CreateVersion7(), orgB, 4000, 400, "USD", paidAt),
        };

        var journalId = LedgerJournalId.New();
        var entries = new[]
        {
            LedgerEntry.Create(journalId, LedgerAccountType.SellerPayablePending, orgA.Value, EntryDirection.Credit, 660, "USD"),
            LedgerEntry.Create(journalId, LedgerAccountType.SellerPayablePending, orgB.Value, EntryDirection.Credit, 440, "USD"),
            LedgerEntry.Create(journalId, LedgerAccountType.PlatformCash, null, EntryDirection.Debit, 1100, "USD"),
        };

        var result = RefundClawback.Compute(snapshots, entries, refundFeeMinor: 100, RefundFeeBearer.Seller, "USD");

        Assert.Equal(660, result.LedgerLines.Single(line => line.OrganizationId == orgA).AmountMinor);
        Assert.Equal(440, result.LedgerLines.Single(line => line.OrganizationId == orgB).AmountMinor);
    }
}

public sealed class RefundJournalPosterTests
{
    [Fact]
    public void PostRefund_creates_balanced_journal_and_platform_fee_bearer_line()
    {
        var purchaseId = PurchaseId.New();
        var org = OrganizationId.New();
        var trackId = Guid.CreateVersion7();
        var paidAt = DateTimeOffset.Parse("2026-06-08T12:00:00+00:00");
        var postedAt = paidAt.AddDays(1);

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

        var snapshots = new[]
        {
            PurchaseAllocationSnapshot.Create(purchaseId, trackId, org, 10_000, 857, "USD", paidAt),
        };

        var journalId = LedgerJournalId.New();
        var ledgerEntries = new[]
        {
            LedgerEntry.Create(journalId, LedgerAccountType.SellerPayablePending, org.Value, EntryDirection.Credit, 857, "USD"),
            LedgerEntry.Create(journalId, LedgerAccountType.PlatformCash, null, EntryDirection.Debit, 857, "USD"),
        };

        var result = JournalPoster.PostRefund(
            purchaseId,
            waterfall,
            snapshots,
            ledgerEntries,
            refundFeeMinor: 25,
            RefundFeeBearer.Platform,
            postedAt);

        Assert.True(result.IsSuccess);
        var journal = result.Value!.Journal;
        var debits = journal.Entries.Where(entry => entry.Direction == EntryDirection.Debit).Sum(entry => entry.AmountMinor);
        var credits = journal.Entries.Where(entry => entry.Direction == EntryDirection.Credit).Sum(entry => entry.AmountMinor);
        Assert.Equal(debits, credits);
        Assert.Contains(
            journal.Entries,
            entry => entry.AccountType == LedgerAccountType.PlatformRevenue
                     && entry.Direction == EntryDirection.Debit
                     && entry.AmountMinor == 25);
        Assert.Contains(
            journal.Entries,
            entry => entry.AccountType == LedgerAccountType.PlatformCash
                     && entry.Direction == EntryDirection.Credit
                     && entry.AmountMinor == 25);
    }

    [Fact]
    public void PostRefund_seller_fee_bearer_increases_seller_clawback()
    {
        var purchaseId = PurchaseId.New();
        var org = OrganizationId.New();
        var trackId = Guid.CreateVersion7();
        var paidAt = DateTimeOffset.Parse("2026-06-08T12:00:00+00:00");

        var waterfall = new PurchaseWaterfallResult(
            GrossMinor: 1000,
            VatMinor: 91,
            NetExVatMinor: 909,
            PlatformFeeMinor: 100,
            PspFeeMinor: 30,
            NetToSellersMinor: 779,
            VatRateBps: 1000,
            PlatformFeeRateBps: 1000,
            Currency: "USD");

        var snapshots = new[]
        {
            PurchaseAllocationSnapshot.Create(purchaseId, trackId, org, 10_000, 779, "USD", paidAt),
        };

        var journalId = LedgerJournalId.New();
        var ledgerEntries = new[]
        {
            LedgerEntry.Create(journalId, LedgerAccountType.SellerPayablePending, org.Value, EntryDirection.Credit, 779, "USD"),
            LedgerEntry.Create(journalId, LedgerAccountType.PlatformCash, null, EntryDirection.Debit, 779, "USD"),
        };

        var result = JournalPoster.PostRefund(
            purchaseId,
            waterfall,
            snapshots,
            ledgerEntries,
            refundFeeMinor: 50,
            RefundFeeBearer.Seller,
            paidAt);

        Assert.True(result.IsSuccess);
        var sellerDebit = result.Value!.Journal.Entries
            .Where(entry => entry.AccountType == LedgerAccountType.SellerPayablePending)
            .Sum(entry => entry.AmountMinor);
        var receivableDebit = result.Value!.Clawback.Receivables.Sum(receivable => receivable.AmountMinor);
        Assert.Equal(779, sellerDebit);
        Assert.Equal(50, receivableDebit);
        Assert.Equal(829, sellerDebit + receivableDebit);
    }
}
