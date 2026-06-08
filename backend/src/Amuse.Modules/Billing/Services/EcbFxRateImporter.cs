using System.Globalization;
using System.Xml.Linq;
using Amuse.Domain.Billing;
using Amuse.Modules.Billing.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Amuse.Modules.Billing.Services;

internal sealed class EcbFxRateImporter(
    BillingDbContext billingDb,
    IHttpClientFactory httpClientFactory,
    IOptions<FxRateImportConfig> options,
    ILogger<EcbFxRateImporter> logger)
{
    public async Task<int> ImportLatestAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        var config = options.Value;
        var client = httpClientFactory.CreateClient(nameof(EcbFxRateImporter));
        using var response = await client.GetAsync(config.EcbDailyUrl, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var document = await XDocument.LoadAsync(stream, LoadOptions.None, cancellationToken);

        var cubes = document
            .Descendants()
            .Where(element => element.Name.LocalName == "Cube" && element.Attribute("currency") is not null)
            .Select(element => new
            {
                Currency = element.Attribute("currency")!.Value.ToUpperInvariant(),
                Rate = decimal.Parse(element.Attribute("rate")!.Value, CultureInfo.InvariantCulture),
            })
            .ToDictionary(item => item.Currency, item => item.Rate, StringComparer.Ordinal);

        if (!cubes.TryGetValue("USD", out var eurToUsd) || eurToUsd <= 0)
        {
            logger.LogWarning("ECB daily feed missing USD rate; skipping import");
            return 0;
        }

        var effectiveAt = document
            .Descendants()
            .Select(element => element.Attribute("time")?.Value)
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

        var effective = effectiveAt is not null
            && DateOnly.TryParseExact(
                effectiveAt,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsedDate)
            ? new DateTimeOffset(parsedDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero)
            : now;

        var imported = 0;
        foreach (var quoteCurrency in config.SupportedQuoteCurrencies
                     .Select(currency => currency.Trim().ToUpperInvariant())
                     .Where(currency => currency.Length == 3)
                     .Distinct(StringComparer.Ordinal))
        {
            if (string.Equals(quoteCurrency, "USD", StringComparison.Ordinal))
                continue;

            if (!cubes.TryGetValue(quoteCurrency, out var eurToQuote) || eurToQuote <= 0)
            {
                logger.LogDebug("ECB daily feed missing {Currency}; skipping pair", quoteCurrency);
                continue;
            }

            var usdPerQuote = eurToQuote / eurToUsd;
            if (usdPerQuote <= 0)
                continue;

            var alreadyImported = await billingDb.FxRates.AsNoTracking()
                .AnyAsync(
                    rate => rate.BaseCurrency == "USD"
                        && rate.QuoteCurrency == quoteCurrency
                        && rate.Source == FxRateSource.EcbDaily
                        && rate.EffectiveAt == effective,
                    cancellationToken);

            if (alreadyImported)
                continue;

            billingDb.FxRates.Add(
                FxRate.Create(
                    "USD",
                    quoteCurrency,
                    usdPerQuote,
                    FxRateSource.EcbDaily,
                    effective,
                    now));

            imported++;
        }

        if (imported > 0)
            await billingDb.SaveChangesAsync(cancellationToken);

        return imported;
    }
}
