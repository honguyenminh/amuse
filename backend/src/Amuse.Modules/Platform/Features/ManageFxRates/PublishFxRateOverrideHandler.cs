using Amuse.Domain.Billing;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Billing.Persistence;
using Amuse.Modules.Common.Time;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Platform.Features.ManageFxRates;

internal sealed class PublishFxRateOverrideHandler(BillingDbContext billingDb, IClock clock)
{
    public async Task<Result<PlatformFxRateRow>> HandleAsync(
        PublishFxRateOverrideRequest request,
        CancellationToken cancellationToken)
    {
        var quoteCurrency = request.QuoteCurrency.Trim().ToUpperInvariant();
        if (quoteCurrency.Length != 3)
            return Result<PlatformFxRateRow>.Failure(BillingErrors.FxRateInvalid);

        if (string.Equals(quoteCurrency, "USD", StringComparison.Ordinal))
            return Result<PlatformFxRateRow>.Failure(BillingErrors.FxRateQuoteMustNotBeUsd);

        if (request.Rate <= 0)
            return Result<PlatformFxRateRow>.Failure(BillingErrors.FxRateInvalid);

        var now = clock.UtcNow;
        var rate = FxRate.Create(
            "USD",
            quoteCurrency,
            request.Rate,
            FxRateSource.OpsManual,
            request.EffectiveAt,
            now);

        billingDb.FxRates.Add(rate);
        await billingDb.SaveChangesAsync(cancellationToken);

        return Result<PlatformFxRateRow>.Success(ToRow(rate));
    }

    internal static PlatformFxRateRow ToRow(FxRate rate) =>
        new(
            rate.Id.Value,
            rate.BaseCurrency,
            rate.QuoteCurrency,
            rate.Rate,
            rate.Source,
            rate.EffectiveAt,
            rate.ImportedAt);
}

internal sealed class ListFxRatesHandler(BillingDbContext billingDb)
{
    public async Task<Result<IReadOnlyList<PlatformFxRateRow>>> HandleAsync(
        string? quoteCurrency,
        CancellationToken cancellationToken)
    {
        var query = billingDb.FxRates.AsNoTracking()
            .Where(rate => rate.BaseCurrency == "USD");

        if (!string.IsNullOrWhiteSpace(quoteCurrency))
        {
            var normalized = quoteCurrency.Trim().ToUpperInvariant();
            query = query.Where(rate => rate.QuoteCurrency == normalized);
        }

        var rows = await query
            .OrderByDescending(rate => rate.EffectiveAt)
            .ThenByDescending(rate => rate.ImportedAt)
            .Take(100)
            .Select(rate => new PlatformFxRateRow(
                rate.Id.Value,
                rate.BaseCurrency,
                rate.QuoteCurrency,
                rate.Rate,
                rate.Source,
                rate.EffectiveAt,
                rate.ImportedAt))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<PlatformFxRateRow>>.Success(rows);
    }
}

public sealed record PlatformFxRateRow(
    Guid Id,
    string BaseCurrency,
    string QuoteCurrency,
    decimal Rate,
    FxRateSource Source,
    DateTimeOffset EffectiveAt,
    DateTimeOffset ImportedAt);
