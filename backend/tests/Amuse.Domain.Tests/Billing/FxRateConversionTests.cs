using Amuse.Domain.Billing;

namespace Amuse.Domain.Tests.Billing;

public sealed class FxRateConversionTests
{
    [Fact]
    public void ToUsdEquivalentMinor_converts_vnd_using_usd_quote_rate()
    {
        var rate = FxRate.Create(
            "USD",
            "VND",
            25_000m,
            FxRateSource.EcbDaily,
            DateTimeOffset.Parse("2026-06-08T00:00:00+00:00"),
            DateTimeOffset.Parse("2026-06-08T00:00:00+00:00"));

        var usdMinor = FxRateConversion.ToUsdEquivalentMinor(250_000, "VND", rate);
        Assert.Equal(10, usdMinor);
    }
}
