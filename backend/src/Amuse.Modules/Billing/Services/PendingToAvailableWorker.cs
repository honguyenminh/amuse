using Amuse.Domain.Billing;
using Amuse.Domain.Tenancy;
using Amuse.Modules.Billing.Persistence;
using Amuse.Modules.Common.Time;
using Amuse.Modules.Tenancy.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Amuse.Modules.Billing.Services;

internal sealed partial class PendingToAvailableWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<BillingSchedulerOptions> options,
    ILogger<PendingToAvailableWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var pollInterval = TimeSpan.FromSeconds(Math.Max(30, options.Value.PollIntervalSeconds));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ReleaseDuePendingCreditsAsync(stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                LogIterationFailed(ex);
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

    internal async Task<int> ReleaseDuePendingCreditsAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var billingDb = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
        var tenancy = scope.ServiceProvider.GetRequiredService<ITenancyOrganizationReadModel>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();
        var now = clock.UtcNow;

        var dueJournals = await billingDb.LedgerJournals.AsNoTracking()
            .Where(j =>
                j.JournalType == JournalType.Purchase
                && j.AvailableAt != null
                && j.AvailableAt <= now)
            .Select(j => new { j.Id, j.ReferenceId, j.Currency, j.AvailableAt })
            .ToListAsync(cancellationToken);

        if (dueJournals.Count == 0)
            return 0;

        var purchaseIds = dueJournals.Select(j => j.ReferenceId).ToArray();
        var alreadyReleased = await billingDb.LedgerJournals.AsNoTracking()
            .Where(j => j.JournalType == JournalType.HoldRelease && purchaseIds.Contains(j.ReferenceId))
            .Select(j => j.ReferenceId)
            .ToListAsync(cancellationToken);

        var released = 0;
        foreach (var due in dueJournals.Where(j => !alreadyReleased.Contains(j.ReferenceId)))
        {
            var pendingCredits = await billingDb.LedgerEntries.AsNoTracking()
                .Where(e =>
                    e.JournalId == due.Id
                    && e.AccountType == LedgerAccountType.SellerPayablePending
                    && e.Direction == EntryDirection.Credit
                    && e.OrganizationId != null)
                .Select(e => new
                {
                    OrganizationId = OrganizationId.From(e.OrganizationId!.Value),
                    e.AmountMinor,
                })
                .ToListAsync(cancellationToken);

            if (pendingCredits.Count == 0)
                continue;

            var filteredCredits = new List<(OrganizationId OrganizationId, long AmountMinor)>();
            foreach (var credit in pendingCredits)
            {
                var lifecycle = await tenancy.GetLifecycleStatusAsync(credit.OrganizationId, cancellationToken);
                if (lifecycle is OrganizationLifecycleStatus.Suspended or OrganizationLifecycleStatus.Closed)
                    continue;

                filteredCredits.Add((credit.OrganizationId, credit.AmountMinor));
            }

            if (filteredCredits.Count == 0)
                continue;

            var journalResult = JournalPoster.PostHoldRelease(
                PurchaseId.From(due.ReferenceId),
                due.Currency,
                now,
                filteredCredits);

            if (!journalResult.IsSuccess)
            {
                LogHoldReleaseSkipped(due.ReferenceId, journalResult.Error?.Code);
                continue;
            }

            billingDb.LedgerJournals.Add(journalResult.Value!);
            await billingDb.SaveChangesAsync(cancellationToken);
            released++;
        }

        if (released > 0)
            LogReleasedPendingCredits(released);

        return released;
    }
}

public sealed class BillingSchedulerOptions
{
    public const string SectionName = "BillingScheduler";

    public int PollIntervalSeconds { get; set; } = 300;
}
