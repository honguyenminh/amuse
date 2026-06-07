namespace Amuse.Modules.Audit;

public sealed record AuditLogEntrySnapshot(
    Guid Id,
    string Action,
    string TableName,
    Guid TargetId,
    string? BeforeJson,
    string? AfterJson,
    DateTimeOffset ChangedAt,
    Guid? ActorAccountId);

public interface IAuditLogReadModel
{
    Task<IReadOnlyList<AuditLogEntrySnapshot>> QueryByTargetAsync(
        string tableName,
        Guid targetId,
        int take,
        CancellationToken cancellationToken);
}
