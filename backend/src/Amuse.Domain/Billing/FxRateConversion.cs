namespace Amuse.Domain.Billing;

public static class FxRateConversion
{
    public static long ToUsdEquivalentMinor(long amountMinor, string currency, FxRate usdQuoteRate)
    {
        if (amountMinor <= 0)
            return 0;

        if (string.Equals(currency, "USD", StringComparison.Ordinal))
            return amountMinor;

        if (!string.Equals(usdQuoteRate.BaseCurrency, "USD", StringComparison.Ordinal)
            || !string.Equals(usdQuoteRate.QuoteCurrency, currency, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"FX rate must be USD/{currency}, got {usdQuoteRate.BaseCurrency}/{usdQuoteRate.QuoteCurrency}.");
        }

        var usdMajor = amountMinor / (decimal)usdQuoteRate.Rate;
        return (long)Math.Floor(usdMajor);
    }
}
