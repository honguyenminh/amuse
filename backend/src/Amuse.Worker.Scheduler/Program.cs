using Amuse.Domain.Catalog;
using Amuse.Modules.Catalog;
using Amuse.Modules.Catalog.Persistence;
using Amuse.Modules.Catalog.Services;
using Amuse.Modules.Common.Time;
using Amuse.Modules.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddCatalogModule(builder.Configuration);
builder.Services.AddTenancyModule(builder.Configuration);
builder.Services.AddSingleton<IClock, SystemClock>();

builder.Services
    .AddOptions<CatalogSchedulerOptions>()
    .Bind(builder.Configuration.GetSection(CatalogSchedulerOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddHostedService<ScheduledReleasePublishWorker>();

await builder.Build().RunAsync();

internal sealed class CatalogSchedulerOptions
{
    public const string SectionName = "CatalogScheduler";

    public int PollIntervalSeconds { get; set; } = 60;
    public int BatchSize { get; set; } = 50;
}

internal sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

internal sealed partial class ScheduledReleasePublishWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<CatalogSchedulerOptions> options,
    ILogger<ScheduledReleasePublishWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var pollInterval = TimeSpan.FromSeconds(Math.Max(5, options.Value.PollIntervalSeconds));
        var batchSize = Math.Max(1, options.Value.BatchSize);

        LogSchedulerStarting(pollInterval.TotalSeconds, batchSize);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PublishDueReleasesAsync(batchSize, stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                LogSchedulerIterationFailed(ex);
            }

            try
            {
                await Task.Delay(pollInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // ignore
            }
        }
    }

    private async Task PublishDueReleasesAsync(int batchSize, CancellationToken stoppingToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();
        var publishing = scope.ServiceProvider.GetRequiredService<ReleasePublishingService>();

        var now = clock.UtcNow;
        var published = 0;
        var skipped = 0;

        var strategy = db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await db.Database.BeginTransactionAsync(stoppingToken);

            var claim = scope.ServiceProvider.GetRequiredService<ScheduledReleaseClaimService>();
            var dueIds = await claim.ClaimDueReleaseIdsAsync(now, batchSize, stoppingToken);
            var due = await claim.LoadClaimedReleasesAsync(dueIds, stoppingToken);

            foreach (var release in due)
            {
                var result = await publishing.PublishSystemAsync(release.Id, stoppingToken);
                if (result.IsSuccess)
                {
                    published++;
                    continue;
                }

                skipped++;
                LogScheduledPublishSkipped(release.Id.Value, result.Error?.Code ?? "unknown");
            }

            await tx.CommitAsync(stoppingToken);
        });

        if (published > 0 || skipped > 0)
        {
            LogSchedulerProcessedDueReleases(published, skipped);
        }
    }
}
