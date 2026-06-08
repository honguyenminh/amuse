using Amuse.Domain.Billing;
using Amuse.Modules.Common.Time;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Amuse.Modules.Billing.Services;

internal sealed class FxRateImportWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<FxRateImportConfig> options,
    ILogger<FxRateImportWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromHours(Math.Max(1, options.Value.RunIntervalHours));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ImportAsync(stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(ex, "FX rate import worker iteration failed");
            }

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // ignore
            }
        }
    }

    internal async Task<int> ImportAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var importer = scope.ServiceProvider.GetRequiredService<EcbFxRateImporter>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();
        var imported = await importer.ImportLatestAsync(clock.UtcNow, cancellationToken);

        if (imported > 0)
            logger.LogInformation("Imported {Count} ECB FX rates", imported);

        return imported;
    }
}
