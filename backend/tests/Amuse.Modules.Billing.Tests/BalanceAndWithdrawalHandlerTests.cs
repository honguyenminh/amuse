using Amuse.Domain.Billing;
using Amuse.Domain.SharedKernel;
using Amuse.Domain.Tenancy;
using Amuse.Modules.Billing.Contracts;
using Amuse.Modules.Billing.Features.Balance;
using Amuse.Modules.Billing.Features.Withdrawals;
using Amuse.Modules.Billing.Persistence;
using Amuse.Modules.Billing.Contracts;
using Amuse.Modules.Billing.Services;
using Amuse.Modules.Common.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Amuse.Modules.Billing.Tests;

public sealed class BalanceAndWithdrawalHandlerTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-06-08T12:00:00+00:00");
    private static readonly OrganizationId OrgId = OrganizationId.New();

    [Fact]
    public async Task GetBalance_returns_per_currency_totals()
    {
        await using var billingDb = CreateBillingDb();
        SeedAvailableBalance(billingDb, 4_500, "USD");

        var handler = new GetBalanceHandler(
            billingDb,
            new LedgerBalanceReadModel(billingDb, new FxRateReadModel(billingDb)),
            new FixedClock(Now),
            Options.Create(new WithdrawalAutoApproveConfig()));

        var result = await handler.HandleAsync(OrgPrincipal(OrgId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var usd = Assert.Single(result.Value!.Balances);
        Assert.Equal(4_500, usd.AvailableMinor);
        Assert.Equal(4_500, usd.UsdEquivalentMinor);
    }

    [Fact]
    public async Task CreateWithdrawal_rejects_below_minimum_usd_equivalent()
    {
        await using var billingDb = CreateBillingDb();
        SeedVerifiedProfile(billingDb);
        SeedAvailableBalance(billingDb, 50_000, "VND");

        var handler = CreateWithdrawalHandler(billingDb);
        var result = await handler.HandleAsync(
            new CreateWithdrawalRequest(50_000, "VND"),
            OrgPrincipal(OrgId),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(BillingErrors.WithdrawalBelowMinimum.Code, result.Error!.Code);
    }

    [Fact]
    public async Task CreateWithdrawal_rejects_when_gate_b_not_verified()
    {
        await using var billingDb = CreateBillingDb();
        SeedAvailableBalance(billingDb, 5_000, "USD");

        var handler = CreateWithdrawalHandler(billingDb);
        var result = await handler.HandleAsync(
            new CreateWithdrawalRequest(2_000, "USD"),
            OrgPrincipal(OrgId),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(BillingErrors.PayoutProfileNotFound.Code, result.Error!.Code);
    }

    [Fact]
    public async Task CreateWithdrawal_rejects_when_receivable_outstanding()
    {
        await using var billingDb = CreateBillingDb();
        SeedVerifiedProfile(billingDb);
        SeedAvailableBalance(billingDb, 5_000, "USD");
        billingDb.SellerReceivables.Add(SellerReceivable.Create(
            OrgId,
            PurchaseId.New(),
            1_000,
            "USD",
            Now));
        await billingDb.SaveChangesAsync();

        var handler = CreateWithdrawalHandler(billingDb);
        var result = await handler.HandleAsync(
            new CreateWithdrawalRequest(2_000, "USD"),
            OrgPrincipal(OrgId),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(BillingErrors.WithdrawalReceivableOutstanding.Code, result.Error!.Code);
    }

    [Fact]
    public async Task CreateWithdrawal_rejects_during_cooldown()
    {
        await using var billingDb = CreateBillingDb();
        SeedVerifiedProfile(billingDb);
        SeedAvailableBalance(billingDb, 10_000, "USD");

        var prior = WithdrawalRequest.CreateForManualRail(
            OrgId,
            Money.Create(2_000, "USD").Value!,
            null,
            Now.AddDays(-2)).Value!;
        prior.MarkApproved();
        prior.MarkProcessing();
        prior.MarkCompleted("REF-1", null, Now.AddDays(-2));
        billingDb.WithdrawalRequests.Add(prior);
        await billingDb.SaveChangesAsync();

        var handler = CreateWithdrawalHandler(billingDb);
        var result = await handler.HandleAsync(
            new CreateWithdrawalRequest(2_000, "USD"),
            OrgPrincipal(OrgId),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(BillingErrors.WithdrawalCooldownActive.Code, result.Error!.Code);
    }

    [Fact]
    public async Task CreateWithdrawal_reserves_available_and_queues_pending_approval()
    {
        await using var billingDb = CreateBillingDb();
        SeedVerifiedProfile(billingDb);
        SeedAvailableBalance(billingDb, 5_000, "USD");

        var handler = CreateWithdrawalHandler(billingDb);
        var result = await handler.HandleAsync(
            new CreateWithdrawalRequest(2_000, "USD"),
            OrgPrincipal(OrgId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(WithdrawalStatus.PendingApproval, result.Value!.Status);

        var withdrawal = await billingDb.WithdrawalRequests.SingleAsync();
        Assert.Equal(2_000, withdrawal.AmountMinor);

        var entries = await billingDb.LedgerEntries.ToListAsync();
        var available = SellerLedgerBalance.NetBalance(
            entries,
            LedgerAccountType.SellerPayableAvailable,
            OrgId.Value,
            "USD");
        var inPayout = SellerLedgerBalance.NetBalance(
            entries,
            LedgerAccountType.SellerPayableInPayout,
            OrgId.Value,
            "USD");

        Assert.Equal(3_000, available);
        Assert.Equal(2_000, inPayout);
    }

    [Fact]
    public async Task CreateWithdrawal_stripe_global_under_threshold_auto_approves()
    {
        await using var billingDb = CreateBillingDb();
        SeedVerifiedStripeProfile(billingDb, externalRecipientId: "acct_test_123");
        SeedAvailableBalance(billingDb, 5_000, "USD");

        var payoutProvider = Substitute.For<IGlobalPayoutProvider>();
        payoutProvider.SubmitOutboundPaymentAsync(Arg.Any<OutboundPaymentRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<OutboundPaymentResult>.Success(new OutboundPaymentResult("tr_test_123")));

        var handler = CreateWithdrawalHandler(billingDb, payoutProvider);
        var result = await handler.HandleAsync(
            new CreateWithdrawalRequest(2_000, "USD"),
            OrgPrincipal(OrgId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(WithdrawalStatus.Completed, result.Value!.Status);
        Assert.Equal("tr_test_123", result.Value.TransferReference);
    }

    [Fact]
    public async Task CreateWithdrawal_stripe_global_above_threshold_queues_pending_approval()
    {
        await using var billingDb = CreateBillingDb();
        SeedVerifiedStripeProfile(billingDb, externalRecipientId: "acct_test_123");
        SeedAvailableBalance(billingDb, 600_000, "USD");

        var handler = CreateWithdrawalHandler(
            billingDb,
            maxAutoApproveUsdMinor: 500_000);
        var result = await handler.HandleAsync(
            new CreateWithdrawalRequest(600_000, "USD"),
            OrgPrincipal(OrgId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(WithdrawalStatus.PendingApproval, result.Value!.Status);
    }

    [Fact]
    public async Task CreateWithdrawal_uses_stored_fx_rate_for_minimum_check()
    {
        await using var billingDb = CreateBillingDb();
        SeedVerifiedProfile(billingDb);
        SeedAvailableBalance(billingDb, 35_000_000, "VND");

        billingDb.FxRates.Add(
            FxRate.Create(
                "USD",
                "VND",
                30_000m,
                FxRateSource.OpsManual,
                Now,
                Now));
        await billingDb.SaveChangesAsync();

        var handler = CreateWithdrawalHandler(billingDb);
        var belowMinimum = await handler.HandleAsync(
            new CreateWithdrawalRequest(250_000, "VND"),
            OrgPrincipal(OrgId),
            CancellationToken.None);

        Assert.False(belowMinimum.IsSuccess);
        Assert.Equal(BillingErrors.WithdrawalBelowMinimum.Code, belowMinimum.Error!.Code);

        var atMinimum = await handler.HandleAsync(
            new CreateWithdrawalRequest(30_000_000, "VND"),
            OrgPrincipal(OrgId),
            CancellationToken.None);

        Assert.True(atMinimum.IsSuccess);
    }

    private static CreateWithdrawalHandler CreateWithdrawalHandler(
        BillingDbContext billingDb,
        IGlobalPayoutProvider? payoutProvider = null,
        long maxAutoApproveUsdMinor = 500_000) =>
        new(
            billingDb,
            new LedgerBalanceReadModel(billingDb, new FxRateReadModel(billingDb)),
            new FxRateReadModel(billingDb),
            new FixedClock(Now),
            Options.Create(new WithdrawalAutoApproveConfig
            {
                CooldownDays = 7,
                MaxAutoApproveUsdMinor = maxAutoApproveUsdMinor,
            }),
            new StripeWithdrawalExecutionService(
                billingDb,
                payoutProvider ?? Substitute.For<IGlobalPayoutProvider>(),
                NullLogger<StripeWithdrawalExecutionService>.Instance));

    private static void SeedVerifiedProfile(BillingDbContext billingDb) =>
        SeedVerifiedStripeProfile(billingDb, externalRecipientId: null, PayoutRail.ManualBank);

    private static void SeedVerifiedStripeProfile(
        BillingDbContext billingDb,
        string? externalRecipientId,
        PayoutRail payoutRail = PayoutRail.StripeGlobal)
    {
        var profile = Amuse.Domain.Billing.PayoutProfile.CreateDraft(
            OrgId,
            LegalEntityType.Individual,
            "Seller LLC",
            Now).Value!;
        profile.ApplyDetails(
            new PayoutProfileDetailsUpdate(
                LegalEntityType.Individual,
                "Seller LLC",
                "123 Main Street",
                null,
                "Ho Chi Minh City",
                null,
                "700000",
                "VN",
                "protected-tax-id",
                null,
                payoutRail,
                "protected-bank-account",
                "1234",
                "Vietcombank",
                ["billing/payout-docs/id.pdf"]),
            Now);
        profile.Submit(Now);
        profile.EnterReview(Now);
        profile.Approve(Amuse.Domain.Identity.AccountId.New(), Now);

        if (!string.IsNullOrWhiteSpace(externalRecipientId))
            profile.SetExternalRecipientId(externalRecipientId, Now);

        billingDb.PayoutProfiles.Add(profile);
        billingDb.SaveChanges();
    }

    private static void SeedAvailableBalance(BillingDbContext billingDb, long amountMinor, string currency)
    {
        var journalId = LedgerJournalId.New();
        var entries = new[]
        {
            LedgerEntry.Create(
                journalId,
                LedgerAccountType.SellerPayableAvailable,
                OrgId.Value,
                EntryDirection.Credit,
                amountMinor,
                currency),
            LedgerEntry.Create(
                journalId,
                LedgerAccountType.PlatformCash,
                null,
                EntryDirection.Debit,
                amountMinor,
                currency),
        };

        var journal = LedgerJournal.Create(
            JournalType.Adjustment,
            ReferenceType.Adjustment,
            Guid.CreateVersion7(),
            currency,
            Now,
            availableAt: null,
            entries).Value!;

        billingDb.LedgerJournals.Add(journal);
        billingDb.SaveChanges();
    }

    private static BillingDbContext CreateBillingDb()
    {
        var options = new DbContextOptionsBuilder<BillingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new BillingDbContext(options);
    }

    private static System.Security.Claims.ClaimsPrincipal OrgPrincipal(OrganizationId orgId)
    {
        var claims = new[]
        {
            new System.Security.Claims.Claim("ctx", "org"),
            new System.Security.Claims.Claim("org_id", orgId.Value.ToString()),
        };
        return new System.Security.Claims.ClaimsPrincipal(
            new System.Security.Claims.ClaimsIdentity(claims, "test"));
    }

    private sealed class FixedClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow => now;
    }
}
