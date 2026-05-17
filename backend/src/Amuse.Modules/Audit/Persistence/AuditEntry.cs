namespace Amuse.Modules.Audit.Persistence;

public sealed class AuditEntry
{
    public Guid Id { get; set; }
    public string Action { get; set; } = null!;
    public string TableName { get; set; } = null!;
    public Guid TargetId { get; set; }
    public string? BeforeJson { get; set; }
    public string? AfterJson { get; set; }
    public DateTimeOffset ChangedAt { get; set; }
    public Guid? ActorAccountId { get; set; }
    public string? Reason { get; set; }
}
