using Amuse.Domain.Billing;
using Amuse.Domain.Identity;
using Amuse.Domain.SharedKernel;
using Amuse.Domain.Tenancy;
using Amuse.Modules.Billing.Contracts;
using Amuse.Modules.Billing.Persistence;
using Amuse.Modules.Billing.Services;
using Amuse.Modules.Catalog.Contracts;
using Amuse.Modules.Common.Time;
using Amuse.Modules.Tenancy.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Amuse.Modules.Billing.Tests;

public sealed class PaidPurchaseCompletionServiceTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-06-08T12:00:00+00:00");
    private static readonly OrganizationId OrgId = OrganizationId.New();
    private static readonly AccountId AccountId = AccountId.New();

    [Fact]
    public async Task CompleteAsync_rejects_banned_payment_fingerprint()
    {
        await using var billingDb = CreateBillingDb();
        billingDb.BannedPaymentInstruments.Add(
            BannedPaymentInstrument.Create("fp_banned_card", "chargeback", Now));
        await billingDb.SaveChangesAsync();

        var trackId = Guid.CreateVersion7();
        var purchase = Purchase.CreatePaidTrack(
            AccountId,
            OrgId,
            trackId,
            Money.Create(1100, "USD").Value!,
            Now).Value!;

        var payment = PaymentTransaction.CreatePending(
            purchase.Id,
            AccountId,
            Money.Create(1100, "USD").Value!,
            Now).Value!;

        billingDb.Purchases.Add(purchase);
        billingDb.PaymentTransactions.Add(payment);
        await billingDb.SaveChangesAsync();

        var catalog = Substitute.For<ICatalogPurchaseReadModel>();
        catalog.GetCheckoutTrackAsync(Arg.Any<Amuse.Domain.Catalog.TrackId>(), Arg.Any<CancellationToken>())
            .Returns(new CatalogCheckoutTrackRow(
                trackId,
                Guid.CreateVersion7(),
                OrgId,
                "Track",
                500,
                null,
                "USD",
                []));

        var tenancy = Substitute.For<ITenancyOrganizationReadModel>();
        tenancy.GetLifecycleStatusAsync(OrgId, Arg.Any<CancellationToken>())
            .Returns(OrganizationLifecycleStatus.Active);

        var service = new PaidPurchaseCompletionService(
            billingDb,
            catalog,
            tenancy,
            Options.Create(new PlatformFeeConfig { DefaultRateBps = 1000 }),
            Options.Create(new TaxConfig { DefaultVatBps = 1000 }),
            Options.Create(new HoldConfig { Days = 3 }),
            new FixedClock(Now));

        var result = await service.CompleteAsync(
            purchase,
            payment,
            new CompletedCheckoutPayment("cs_test", "ch_test", "fp_banned_card", 33, 1100, "USD"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(BillingErrors.PaymentInstrumentBanned, result.Error);
        Assert.Equal(PaymentStatus.Pending, purchase.PaymentStatus);
    }

    private static BillingDbContext CreateBillingDb()
    {
        var options = new DbContextOptionsBuilder<BillingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new BillingDbContext(options);
    }

    private sealed class FixedClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow => now;
    }
}
