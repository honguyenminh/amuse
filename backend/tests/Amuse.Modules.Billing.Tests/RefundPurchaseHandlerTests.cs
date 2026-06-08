using Amuse.Domain.Billing;
using Amuse.Domain.Identity;
using Amuse.Domain.SharedKernel;
using Amuse.Domain.Tenancy;
using Amuse.Modules.Billing.Contracts;
using Amuse.Modules.Billing.Features.RefundPurchase;
using Amuse.Modules.Billing.Persistence;
using Amuse.Modules.Billing.Services;
using Amuse.Modules.Common.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Amuse.Modules.Billing.Tests;

public sealed class RefundPurchaseHandlerTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-06-08T12:00:00+00:00");
    private static readonly OrganizationId OrgId = OrganizationId.New();
    private static readonly AccountId AccountId = AccountId.New();

    [Fact]
    public async Task HandleAsync_platform_initiated_requires_fee_bearer()
    {
        await using var billingDb = CreateBillingDb();
        var purchase = SeedPaidPurchase(billingDb);

        var handler = CreateHandler(billingDb, refundFeeMinor: 0);
        var result = await handler.HandleAsync(
            purchase.Id.Value,
            new RefundPurchaseRequest("Customer complaint", null),
            PlatformPrincipal(),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(BillingErrors.RefundFeeBearerRequired.Code, result.Error!.Code);
    }

    [Fact]
    public async Task HandleAsync_seller_initiated_always_uses_seller_fee_bearer()
    {
        await using var billingDb = CreateBillingDb();
        var purchase = SeedPaidPurchase(billingDb);

        var handler = CreateHandler(billingDb, refundFeeMinor: 40);
        var result = await handler.HandleAsync(
            purchase.Id.Value,
            new RefundPurchaseRequest("Fan support refund", "platform"),
            OrgPrincipal(OrgId, ["manage:purchase:refund:all"]),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Code);
        Assert.Equal("refunded", result.Value!.PaymentStatus);
        Assert.Equal(RefundFeeBearer.Seller, purchase.RefundFeeBearer);

        var refundJournal = await billingDb.LedgerJournals
            .SingleAsync(journal => journal.JournalType == JournalType.Refund);
        var sellerDebit = refundJournal.Entries
            .Where(entry => entry.AccountType == LedgerAccountType.SellerPayablePending)
            .Sum(entry => entry.AmountMinor);
        var liabilityDebit = refundJournal.Entries
            .Where(entry => entry.AccountType == LedgerAccountType.RefundLiability)
            .Sum(entry => entry.AmountMinor);
        Assert.Equal(857, sellerDebit);
        Assert.Equal(40, liabilityDebit);
    }

    [Fact]
    public async Task HandleAsync_platform_fee_bearer_debits_platform_revenue_for_refund_fee()
    {
        await using var billingDb = CreateBillingDb();
        var purchase = SeedPaidPurchase(billingDb);

        var handler = CreateHandler(billingDb, refundFeeMinor: 30);
        var result = await handler.HandleAsync(
            purchase.Id.Value,
            new RefundPurchaseRequest("Operator override", "platform"),
            PlatformPrincipal(),
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        var refundJournal = await billingDb.LedgerJournals
            .SingleAsync(journal => journal.JournalType == JournalType.Refund);
        Assert.Contains(
            refundJournal.Entries,
            entry => entry.AccountType == LedgerAccountType.PlatformRevenue
                     && entry.Direction == EntryDirection.Debit
                     && entry.AmountMinor == 30);
    }

    [Fact]
    public async Task HandleAsync_rejects_seller_not_on_allocation_snapshot()
    {
        await using var billingDb = CreateBillingDb();
        var purchase = SeedPaidPurchase(billingDb);
        var otherOrg = OrganizationId.New();

        var handler = CreateHandler(billingDb, refundFeeMinor: 0);
        var result = await handler.HandleAsync(
            purchase.Id.Value,
            new RefundPurchaseRequest("Not allowed", null),
            OrgPrincipal(otherOrg, ["manage:purchase:refund:all"]),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(BillingErrors.RefundNotAllowed.Code, result.Error!.Code);
    }

    private static RefundPurchaseHandler CreateHandler(BillingDbContext billingDb, long refundFeeMinor)
    {
        var checkoutProvider = Substitute.For<ICheckoutProvider>();
        checkoutProvider.RefundChargeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<RefundChargeResult>.Success(new RefundChargeResult("re_123", refundFeeMinor)));

        var completionService = new RefundCompletionService(
            billingDb,
            Options.Create(new PlatformFeeConfig { DefaultRateBps = 1000 }),
            Options.Create(new TaxConfig { DefaultVatBps = 1000 }),
            new FixedClock(Now));

        return new RefundPurchaseHandler(
            billingDb,
            checkoutProvider,
            completionService,
            new FixedClock(Now));
    }

    private static Purchase SeedPaidPurchase(BillingDbContext billingDb)
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
        payment.MarkCompleted("ch_test", null, 33, Now);

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
        return purchase;
    }

    private static BillingDbContext CreateBillingDb()
    {
        var options = new DbContextOptionsBuilder<BillingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new BillingDbContext(options);
    }

    private static System.Security.Claims.ClaimsPrincipal PlatformPrincipal()
    {
        var claims = new[]
        {
            new System.Security.Claims.Claim("ctx", "platform"),
            new System.Security.Claims.Claim("sub", AccountId.Value.ToString()),
            new System.Security.Claims.Claim("claims", "manage:platform:purchases:all"),
        };
        return new System.Security.Claims.ClaimsPrincipal(
            new System.Security.Claims.ClaimsIdentity(claims, "test"));
    }

    private static System.Security.Claims.ClaimsPrincipal OrgPrincipal(
        OrganizationId orgId,
        string[] orgClaims)
    {
        var claims = new List<System.Security.Claims.Claim>
        {
            new("ctx", "org"),
            new("org_id", orgId.Value.ToString()),
            new("sub", AccountId.Value.ToString()),
        };
        claims.AddRange(orgClaims.Select(claim => new System.Security.Claims.Claim("claims", claim)));
        return new System.Security.Claims.ClaimsPrincipal(
            new System.Security.Claims.ClaimsIdentity(claims, "test"));
    }

    private sealed class FixedClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow => now;
    }
}
