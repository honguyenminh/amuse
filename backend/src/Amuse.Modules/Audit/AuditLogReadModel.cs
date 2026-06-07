using Amuse.Modules.Audit.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Audit;

internal sealed class AuditLogReadModel(AuditDbContext db) : IAuditLogReadModel
{
    public async Task<IReadOnlyList<AuditLogEntrySnapshot>> QueryByTargetAsync(
        string tableName,
        Guid targetId,
        int take,
        CancellationToken cancellationToken)
    {
        var limit = take <= 0 ? 100 : Math.Min(take, 100);

        return await db.AuditEntries
            .AsNoTracking()
            .Where(entry => entry.TableName == tableName && entry.TargetId == targetId)
            .OrderByDescending(entry => entry.ChangedAt)
            .Take(limit)
            .Select(entry => new AuditLogEntrySnapshot(
                entry.Id,
                entry.Action,
                entry.TableName,
                entry.TargetId,
                entry.BeforeJson,
                entry.AfterJson,
                entry.ChangedAt,
                entry.ActorAccountId))
            .ToListAsync(cancellationToken);
    }
}
