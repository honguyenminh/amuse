using Amuse.Modules.Catalog.Persistence;
using Amuse.Modules.Catalog.Processing;
using Amuse.Modules.Common.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Amuse.Modules.Catalog.Messaging;

public sealed partial class TranscodeJobStaleSweeper(
    IServiceScopeFactory scopeFactory,
    IOptions<TranscodeJobRecoveryOptions> options,
    ILogger<TranscodeJobStaleSweeper> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SweepAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                LogSweepFailed(ex);
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    internal async Task<int> SweepAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();
        var now = clock.UtcNow;
        var cutoff = now - options.Value.StaleProcessingTimeout;

        var stale = await db.AudioTranscodeJobs
            .Where(j => j.Status == AudioTranscodeJobStatus.Processing && j.UpdatedAt < cutoff)
            .ToListAsync(cancellationToken);

        foreach (var job in stale)
            job.MarkFailed("Timed out", now);

        if (stale.Count > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
            LogStaleJobsMarkedFailed(stale.Count);
        }

        return stale.Count;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Transcode stale-job sweep failed")]
    private partial void LogSweepFailed(Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Marked {Count} stale transcode jobs as failed")]
    private partial void LogStaleJobsMarkedFailed(int count);
}
