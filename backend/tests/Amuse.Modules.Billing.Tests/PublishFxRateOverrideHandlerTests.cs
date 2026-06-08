using Amuse.Domain.Billing;
using Amuse.Modules.Billing.Persistence;
using Amuse.Modules.Billing.Services;
using Amuse.Modules.Platform.Features.ManageFxRates;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Billing.Tests;

public sealed class PublishFxRateOverrideHandlerTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-06-08T12:00:00+00:00");

    [Fact]
    public async Task PublishFxRateOverride_persists_ops_manual_rate()
    {
        await using var billingDb = CreateBillingDb();
        var handler = new PublishFxRateOverrideHandler(billingDb, new FixedClock(Now));

        var result = await handler.HandleAsync(
            new PublishFxRateOverrideRequest("VND", 28_500m, Now),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(FxRateSource.OpsManual, result.Value!.Source);
        Assert.Equal(28_500m, result.Value.Rate);

        var stored = await billingDb.FxRates.SingleAsync();
        Assert.Equal("VND", stored.QuoteCurrency);
    }

    [Fact]
    public async Task FxRateReadModel_prefers_ops_manual_over_ecb_daily()
    {
        await using var billingDb = CreateBillingDb();
        billingDb.FxRates.AddRange(
            FxRate.Create("USD", "VND", 25_000m, FxRateSource.EcbDaily, Now.AddDays(-1), Now.AddDays(-1)),
            FxRate.Create("USD", "VND", 30_000m, FxRateSource.OpsManual, Now, Now));
        await billingDb.SaveChangesAsync();

        var readModel = new FxRateReadModel(billingDb);
        var result = await readModel.GetUsdEquivalentAsync("VND", 30_000_000, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(FxRateSource.OpsManual, result.Value!.Rate.Source);
        Assert.Equal(1_000, result.Value.UsdEquivalentMinor);
    }

    private static BillingDbContext CreateBillingDb()
    {
        var options = new DbContextOptionsBuilder<BillingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new BillingDbContext(options);
    }

    private sealed class FixedClock(DateTimeOffset now) : Amuse.Modules.Common.Time.IClock
    {
        public DateTimeOffset UtcNow => now;
    }
}
