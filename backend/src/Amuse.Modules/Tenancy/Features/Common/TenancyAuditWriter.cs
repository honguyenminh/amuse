using System.Text.Json;
using System.Text.Json.Serialization;
using Amuse.Modules.Audit;
using Amuse.Modules.Audit.Persistence;
using Amuse.Modules.Common.Time;

namespace Amuse.Modules.Tenancy.Features.Common;

internal sealed class TenancyAuditWriter(IAuditWriter auditWriter, IClock clock)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    internal async Task WriteCreateAsync<T>(
        string tableName,
        Guid targetId,
        T after,
        Guid? actorAccountId,
        CancellationToken cancellationToken)
    {
        await auditWriter.WriteAsync(new AuditEntry
        {
            Id = Guid.CreateVersion7(),
            Action = "created",
            TableName = tableName,
            TargetId = targetId,
            AfterJson = Serialize(after),
            ChangedAt = clock.UtcNow,
            ActorAccountId = actorAccountId,
        }, cancellationToken);
    }

    internal async Task WriteUpdateAsync<T>(
        string tableName,
        Guid targetId,
        T before,
        T after,
        Guid? actorAccountId,
        CancellationToken cancellationToken)
    {
        await auditWriter.WriteAsync(new AuditEntry
        {
            Id = Guid.CreateVersion7(),
            Action = "updated",
            TableName = tableName,
            TargetId = targetId,
            BeforeJson = Serialize(before),
            AfterJson = Serialize(after),
            ChangedAt = clock.UtcNow,
            ActorAccountId = actorAccountId,
        }, cancellationToken);
    }

    private static string Serialize<T>(T value) =>
        JsonSerializer.Serialize(value, JsonOptions);
}
