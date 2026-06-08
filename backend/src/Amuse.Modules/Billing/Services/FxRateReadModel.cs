using Amuse.Domain.Billing;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Billing.Contracts;
using Amuse.Modules.Billing.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Billing.Services;

internal sealed class FxRateReadModel(BillingDbContext billingDb) : IFxRateReadModel
{
    private static readonly IReadOnlyDictionary<string, decimal> EcbStubRates = new Dictionary<string, decimal>(
        StringComparer.Ordinal)
    {
        ["VND"] = 25_000m,
        ["EUR"] = 0.92m,
        ["GBP"] = 0.79m,
    };

    public async Task<Result<(FxRate Rate, long UsdEquivalentMinor)>> GetUsdEquivalentAsync(
        string currency,
        long amountMinor,
        CancellationToken cancellationToken)
    {
        if (amountMinor <= 0)
            return Result<(FxRate, long)>.Success((CreateStubRate(currency, 1m), 0));

        var normalizedCurrency = currency.Trim().ToUpperInvariant();
        if (normalizedCurrency == "USD")
        {
            var usdRate = CreateStubRate("USD", 1m);
            return Result<(FxRate, long)>.Success((usdRate, amountMinor));
        }

        var storedRate = await billingDb.FxRates.AsNoTracking()
            .Where(rate =>
                rate.BaseCurrency == "USD"
                && rate.QuoteCurrency == normalizedCurrency)
            .OrderByDescending(rate => rate.Source == FxRateSource.OpsManual)
            .ThenByDescending(rate => rate.EffectiveAt)
            .ThenByDescending(rate => rate.ImportedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (storedRate is not null)
        {
            var equivalent = FxRateConversion.ToUsdEquivalentMinor(amountMinor, normalizedCurrency, storedRate);
            return Result<(FxRate, long)>.Success((storedRate, equivalent));
        }

        if (!EcbStubRates.TryGetValue(normalizedCurrency, out var stubRate))
            return Result<(FxRate, long)>.Failure(BillingErrors.FxRateNotFound);

        var fallback = CreateStubRate(normalizedCurrency, stubRate);
        var fallbackEquivalent = FxRateConversion.ToUsdEquivalentMinor(amountMinor, normalizedCurrency, fallback);
        return Result<(FxRate, long)>.Success((fallback, fallbackEquivalent));
    }

    private static FxRate CreateStubRate(string quoteCurrency, decimal rate) =>
        FxRate.Create(
            "USD",
            quoteCurrency,
            rate,
            FxRateSource.EcbDaily,
            DateTimeOffset.Parse("2026-06-08T00:00:00+00:00"),
            DateTimeOffset.Parse("2026-06-08T00:00:00+00:00"));
}
