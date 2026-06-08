using Amuse.Domain.Billing;
using Amuse.Domain.Identity;
using Amuse.Domain.SharedKernel;
using Amuse.Domain.Tenancy;
using Amuse.Modules.Billing.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Billing.Tests;

public sealed class BillingDbContextTests
{
    [Fact]
    public void BillingDbContext_exposes_all_phase_zero_entities()
    {
        var options = new DbContextOptionsBuilder<BillingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var db = new BillingDbContext(options);

        Assert.NotNull(db.Purchases);
        Assert.NotNull(db.PaymentTransactions);
        Assert.NotNull(db.LedgerJournals);
        Assert.NotNull(db.PayoutProfiles);
        Assert.NotNull(db.WithdrawalRequests);
        Assert.NotNull(db.TaxInvoices);
        Assert.NotNull(db.BannedPaymentInstruments);
        Assert.NotNull(db.SellerReceivables);
        Assert.NotNull(db.FxRates);
    }

    [Fact]
    public void Purchase_free_track_persists_with_active_entitlement()
    {
        var options = new DbContextOptionsBuilder<BillingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var now = DateTimeOffset.Parse("2026-06-08T12:00:00+00:00");
        var accountId = AccountId.New();
        var orgId = OrganizationId.New();
        var trackId = Guid.CreateVersion7();

        var purchase = Purchase.AcquireFreeTrack(
            accountId,
            orgId,
            trackId,
            Money.Create(0, "USD").Value!,
            now).Value!;

        using var db = new BillingDbContext(options);
        db.Purchases.Add(purchase);
        db.SaveChanges();

        var loaded = db.Purchases.Single();
        Assert.Equal(PaymentStatus.Free, loaded.PaymentStatus);
        Assert.Equal(EntitlementStatus.Active, loaded.EntitlementStatus);
        Assert.Equal(trackId, loaded.TrackId);
    }

    [Fact]
    public void LedgerJournal_rejects_unbalanced_entries()
    {
        var journalId = LedgerJournalId.New();
        var entries = new[]
        {
            LedgerEntry.Create(journalId, LedgerAccountType.PlatformCash, null, EntryDirection.Debit, 100, "USD"),
            LedgerEntry.Create(journalId, LedgerAccountType.VatPayable, null, EntryDirection.Credit, 50, "USD"),
        };

        var result = LedgerJournal.Create(
            JournalType.Purchase,
            ReferenceType.Purchase,
            Guid.CreateVersion7(),
            "USD",
            DateTimeOffset.UtcNow,
            null,
            entries);

        Assert.False(result.IsSuccess);
        Assert.Equal(BillingErrors.InvalidLedgerJournal, result.Error);
    }
}
