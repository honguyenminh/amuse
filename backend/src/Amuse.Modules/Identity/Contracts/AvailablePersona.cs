namespace Amuse.Modules.Identity.Contracts;

public sealed record AvailablePersona(
    string Type,
    Guid? OrgId,
    Guid? ListenerId,
    string? Label);
