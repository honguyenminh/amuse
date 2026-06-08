using Amuse.Domain.Billing;
using Amuse.Domain.Identity;
using Amuse.Domain.SharedKernel;
using Amuse.Domain.Tenancy;
using Amuse.Modules.Audit;
using Amuse.Modules.Audit.Persistence;
using Amuse.Modules.Billing.Persistence;
using Amuse.Modules.Billing.Services;
using Amuse.Modules.Common.Time;
using Amuse.Modules.Identity.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Amuse.Modules.Billing.Tests;

public sealed class ChargebackHandlerTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-06-08T12:00:00+00:00");
    private static readonly OrganizationId OrgId = OrganizationId.New();
    private static readonly AccountId AccountId = AccountId.New();

    [Fact]
    public async Task CompleteAsync_bans_account_instrument_reverses_ledger_and_audits()
    {
        await using var billingDb = CreateBillingDb();
        await using var identityDb = CreateIdentityDb();
        var (purchase, payment) = SeedPaidPurchase(billingDb, fingerprint: "fp_chargeback_test");

        var auditWriter = Substitute.For<IAuditWriter>();
        var service = CreateService(billingDb, identityDb, auditWriter);

        var result = await service.CompleteAsync(
            purchase,
            payment,
            new ChargebackDisputeDetails("dp_test_1", 1100, "usd", "fraudulent", 15),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Code);

        var account = await identityDb.Accounts.SingleAsync();
        Assert.Equal(AccountStatus.Banned, account.Status);

        var bannedInstrument = await billingDb.BannedPaymentInstruments.SingleAsync();
        Assert.Equal("fp_chargeback_test", bannedInstrument.PaymentMethodFingerprint);

        Assert.Equal(PaymentStatus.ChargedBack, purchase.PaymentStatus);
        Assert.Equal(EntitlementStatus.Revoked, purchase.EntitlementStatus);

        var chargebackJournal = await billingDb.LedgerJournals
            .SingleAsync(journal => journal.JournalType == JournalType.Chargeback);
        Assert.Contains(
            chargebackJournal.Entries,
            entry => entry.AccountType == LedgerAccountType.SellerPayablePending
                     && entry.Direction == EntryDirection.Debit);

        await auditWriter.Received(1).WriteAsync(
            Arg.Is<AuditEntry>(entry =>
                entry.Action == "chargeback_received"
                && entry.TableName == "billing.purchase"
                && entry.TargetId == purchase.Id.Value
                && entry.AfterJson!.Contains("dp_test_1")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CompleteAsync_is_idempotent_on_duplicate_dispute_webhook()
    {
        await using var billingDb = CreateBillingDb();
        await using var identityDb = CreateIdentityDb();
        var (purchase, payment) = SeedPaidPurchase(billingDb, fingerprint: "fp_idempotent");

        var auditWriter = Substitute.For<IAuditWriter>();
        var service = CreateService(billingDb, identityDb, auditWriter);
        var dispute = new ChargebackDisputeDetails("dp_test_2", 1100, "usd", "fraudulent", 0);

        var first = await service.CompleteAsync(purchase, payment, dispute, CancellationToken.None);
        var second = await service.CompleteAsync(purchase, payment, dispute, CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Single(await billingDb.BannedPaymentInstruments.ToListAsync());
        Assert.Single(await billingDb.LedgerJournals.Where(j => j.JournalType == JournalType.Chargeback).ToListAsync());
        await auditWriter.Received(1).WriteAsync(
            Arg.Is<AuditEntry>(entry => entry.Action == "chargeback_received"),
            Arg.Any<CancellationToken>());
    }

    private static ChargebackCompletionService CreateService(
        BillingDbContext billingDb,
        IdentityDbContext identityDb,
        IAuditWriter auditWriter) =>
        new(
            billingDb,
            identityDb,
            auditWriter,
            Options.Create(new PlatformFeeConfig { DefaultRateBps = 1000 }),
            Options.Create(new TaxConfig { DefaultVatBps = 1000 }),
            new FixedClock(Now));

    private static (Purchase Purchase, PaymentTransaction Payment) SeedPaidPurchase(
        BillingDbContext billingDb,
        string fingerprint)
    {
        var purchase = Purchase.CreatePaidTrack(
            AccountId,
            OrgId,
            Guid.CreateVersion7(),
            Money.Create(1100, "USD").Value!,
            Now).Value!;

        purchase.MarkPaid(PaymentTransactionId.New(), Now);

        var payment = PaymentTransaction.CreatePending(
            purchase.Id,
            AccountId,
            Money.Create(1100, "USD").Value!,
            Now).Value!;
        payment.MarkCompleted("ch_test", fingerprint, 33, Now);

        var waterfall = PurchaseWaterfall.Compute(1100, "USD", 33, 1000, 1000).Value!;
        var allocation = PurchaseAllocation.AllocateTrack(
            purchase.TrackId!.Value,
            OrgId,
            waterfall.NetToSellersMinor,
            []).Value!;

        var journalResult = JournalPoster.PostPurchase(
            purchase.Id,
            waterfall,
            allocation,
            Now,
            holdDays: 3).Value!;

        billingDb.Purchases.Add(purchase);
        billingDb.PaymentTransactions.Add(payment);
        billingDb.LedgerJournals.Add(journalResult.Journal);
        billingDb.PurchaseAllocationSnapshots.AddRange(journalResult.Snapshots);
        billingDb.TaxInvoices.Add(TaxInvoice.Issue(
            "AM-2026-000001",
            purchase.Id,
            AccountId,
            waterfall.GrossMinor,
            waterfall.VatMinor,
            waterfall.NetExVatMinor,
            waterfall.Currency,
            waterfall.VatRateBps,
            Now));
        billingDb.SaveChanges();
        return (purchase, payment);
    }

    private static BillingDbContext CreateBillingDb()
    {
        var options = new DbContextOptionsBuilder<BillingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new BillingDbContext(options);
    }

    private static IdentityDbContext CreateIdentityDb()
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var db = new IdentityDbContext(options);
        db.Accounts.Add(Account.CreateWithId(
            AccountId,
            IdpIssuer.From("test"),
            IdpSubject.From("subject-chargeback")));
        db.SaveChanges();
        return db;
    }

    private sealed class FixedClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow => now;
    }
}
