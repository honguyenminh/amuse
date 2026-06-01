using Amuse.Domain.Catalog;
using Amuse.Modules.Catalog.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Catalog.Services;

/// <summary>
/// Claims scheduled releases that are due for publishing using row locks safe for
/// multiple scheduler replicas (<c>FOR UPDATE SKIP LOCKED</c>).
/// </summary>
public sealed class ScheduledReleaseClaimService(CatalogDbContext db)
{
    /// <summary>
    /// Selects and locks due scheduled release ids. Must run inside an open transaction on
    /// <paramref name="db"/> so locks are held until commit.
    /// </summary>
    public async Task<IReadOnlyList<ReleaseId>> ClaimDueReleaseIdsAsync(
        DateTimeOffset now,
        int batchSize,
        CancellationToken cancellationToken)
    {
        if (db.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException(
                $"{nameof(ClaimDueReleaseIdsAsync)} must run inside an explicit database transaction.");
        }

        var limit = Math.Max(1, batchSize);

        var dueIds = await db.Database
            .SqlQuery<Guid>($"""
                SELECT r.id
                FROM catalog.release AS r
                WHERE r.lifecycle_status = 'scheduled'::catalog.release_lifecycle_status
                  AND r.release_date <= {now}
                ORDER BY r.release_date
                LIMIT {limit}
                FOR UPDATE SKIP LOCKED
                """)
            .ToListAsync(cancellationToken);

        return dueIds.Select(ReleaseId.From).ToList();
    }

    /// <summary>
    /// Loads releases (with tracks) for ids returned from <see cref="ClaimDueReleaseIdsAsync"/>,
    /// preserving claim order.
    /// </summary>
    public async Task<IReadOnlyList<Release>> LoadClaimedReleasesAsync(
        IReadOnlyList<ReleaseId> releaseIds,
        CancellationToken cancellationToken)
    {
        if (releaseIds.Count == 0)
            return [];

        var releases = await db.Releases
            .Include(r => r.Tracks)
            .Where(r => releaseIds.Contains(r.Id))
            .ToListAsync(cancellationToken);

        var order = releaseIds
            .Select((id, index) => (id.Value, index))
            .ToDictionary(pair => pair.Value, pair => pair.index);

        return releases
            .OrderBy(r => order[r.Id.Value])
            .ToList();
    }
}
