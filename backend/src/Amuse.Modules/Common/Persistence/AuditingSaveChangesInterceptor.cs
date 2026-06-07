using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using Amuse.Modules.Common.Time;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Amuse.Modules.Common.Persistence;

public sealed class AuditingSaveChangesInterceptor(
    AuditEntityRegistry auditEntityRegistry,
    IClock clock,
    IHttpContextAccessor httpContextAccessor) : SaveChangesInterceptor
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
            await WriteAuditEntriesAsync(eventData.Context, cancellationToken);

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        if (eventData.Context is not null)
            WriteAuditEntriesAsync(eventData.Context, CancellationToken.None).GetAwaiter().GetResult();

        return base.SavingChanges(eventData, result);
    }

    private async Task WriteAuditEntriesAsync(DbContext context, CancellationToken cancellationToken)
    {
        var actorAccountId = ResolveActorAccountId();
        var changedAt = clock.UtcNow;

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
                continue;

            if (!auditEntityRegistry.TryGetRegistration(entry.Entity.GetType(), out var registration))
                continue;

            var action = entry.State switch
            {
                EntityState.Added => "created",
                EntityState.Modified => "updated",
                EntityState.Deleted => "deleted",
                _ => throw new InvalidOperationException("Unexpected entity state."),
            };

            var targetId = registration.TargetIdResolver(entry.Entity);
            var beforeJson = entry.State is EntityState.Added
                ? null
                : SerializeEntity(entry.OriginalValues);
            var afterJson = entry.State is EntityState.Deleted
                ? null
                : SerializeEntity(entry.CurrentValues);

            await context.Database.ExecuteSqlInterpolatedAsync(
                $"""
                 INSERT INTO audit.audit_log
                     (id, action, table_name, target_id, before_json, after_json, changed_at, actor_account_id)
                 VALUES
                     ({Guid.CreateVersion7()}, {action}, {registration.TableName}, {targetId},
                      CAST({beforeJson} AS jsonb), CAST({afterJson} AS jsonb), {changedAt}, {actorAccountId})
                 """,
                cancellationToken);
        }
    }

    private Guid? ResolveActorAccountId()
    {
        var user = httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
            return null;

        var sub = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue("sub");

        return Guid.TryParse(sub, out var accountId) ? accountId : null;
    }

    private static string? SerializeEntity(PropertyValues values)
    {
        var payload = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var property in values.Properties)
        {
            payload[property.Name] = values[property.Name];
        }

        return JsonSerializer.Serialize(payload, JsonOptions);
    }
}
