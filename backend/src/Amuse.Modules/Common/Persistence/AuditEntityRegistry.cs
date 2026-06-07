using System.Collections.Concurrent;
using System.Reflection;

namespace Amuse.Modules.Common.Persistence;

public sealed class AuditEntityRegistry
{
    private readonly ConcurrentDictionary<Type, AuditEntityRegistration> _registrations = new();

    public AuditEntityRegistry Register<TEntity>(string tableName, Func<TEntity, Guid>? targetIdResolver = null)
        where TEntity : class
    {
        _registrations[typeof(TEntity)] = new AuditEntityRegistration(
            tableName,
            entity => targetIdResolver is null
                ? AuditEntityIdExtractor.Extract(entity)
                : targetIdResolver((TEntity)entity));

        return this;
    }

    public bool TryGetRegistration(Type entityType, out AuditEntityRegistration registration) =>
        _registrations.TryGetValue(entityType, out registration!);
}

public sealed record AuditEntityRegistration(string TableName, Func<object, Guid> TargetIdResolver);

internal static class AuditEntityIdExtractor
{
    public static Guid Extract(object entity)
    {
        var idProperty = entity.GetType().GetProperty(
            "Id",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);

        if (idProperty is null)
            throw new InvalidOperationException($"Auditable entity '{entity.GetType().Name}' has no Id property.");

        var idValue = idProperty.GetValue(entity)
            ?? throw new InvalidOperationException($"Auditable entity '{entity.GetType().Name}' Id is null.");

        if (idValue is Guid guid)
            return guid;

        var valueProperty = idValue.GetType().GetProperty(
            "Value",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);

        if (valueProperty?.GetValue(idValue) is Guid typedGuid)
            return typedGuid;

        throw new InvalidOperationException(
            $"Auditable entity '{entity.GetType().Name}' Id type '{idValue.GetType().Name}' is not supported.");
    }
}
