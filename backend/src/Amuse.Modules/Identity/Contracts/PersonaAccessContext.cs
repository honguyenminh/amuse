namespace Amuse.Modules.Identity.Contracts;

public sealed record PersonaAccessContext(
    string ContextType,
    Guid? OrgId,
    Guid? ListenerId,
    string? OrgRoleLabel,
    IReadOnlyList<string> Claims);
