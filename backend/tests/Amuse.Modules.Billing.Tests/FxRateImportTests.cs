using Amuse.Domain.Billing;
using Amuse.Modules.Billing.Persistence;
using Amuse.Modules.Billing.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Amuse.Modules.Billing.Tests;

public sealed class FxRateImportTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-06-08T12:00:00+00:00");

    private const string SampleEcbXml = """
        <?xml version="1.0" encoding="UTF-8"?>
        <gesmes:Envelope xmlns:gesmes="http://www.gesmes.org/xml/EN-v2">
          <Cube>
            <Cube time='2026-06-07'>
              <Cube currency='USD' rate='1.1000'/>
              <Cube currency='VND' rate='27000'/>
              <Cube currency='GBP' rate='0.8500'/>
            </Cube>
          </Cube>
        </gesmes:Envelope>
        """;

    [Fact]
    public async Task ImportLatestAsync_persists_usd_quote_rates_from_ecb_feed()
    {
        await using var billingDb = CreateBillingDb();
        var handler = new TestHttpMessageHandler(SampleEcbXml);
        var httpClient = new HttpClient(handler);
        var factory = new FixedHttpClientFactory(httpClient);

        var importer = new EcbFxRateImporter(
            billingDb,
            factory,
            Options.Create(new FxRateImportConfig
            {
                SupportedQuoteCurrencies = ["VND", "GBP"],
            }),
            NullLogger<EcbFxRateImporter>.Instance);

        var imported = await importer.ImportLatestAsync(Now, CancellationToken.None);

        Assert.Equal(2, imported);

        var vndRate = await billingDb.FxRates.SingleAsync(rate => rate.QuoteCurrency == "VND");
        Assert.Equal("USD", vndRate.BaseCurrency);
        Assert.Equal(FxRateSource.EcbDaily, vndRate.Source);
        Assert.True(vndRate.Rate > 20_000m);
    }

    [Fact]
    public async Task ImportLatestAsync_is_idempotent_for_same_effective_date()
    {
        await using var billingDb = CreateBillingDb();
        var handler = new TestHttpMessageHandler(SampleEcbXml);
        var httpClient = new HttpClient(handler);
        var factory = new FixedHttpClientFactory(httpClient);

        var importer = new EcbFxRateImporter(
            billingDb,
            factory,
            Options.Create(new FxRateImportConfig { SupportedQuoteCurrencies = ["VND"] }),
            NullLogger<EcbFxRateImporter>.Instance);

        Assert.Equal(1, await importer.ImportLatestAsync(Now, CancellationToken.None));
        Assert.Equal(0, await importer.ImportLatestAsync(Now, CancellationToken.None));
    }

    private static BillingDbContext CreateBillingDb()
    {
        var options = new DbContextOptionsBuilder<BillingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new BillingDbContext(options);
    }

    private sealed class TestHttpMessageHandler(string content) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(content),
            });
    }

    private sealed class FixedHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }
}
