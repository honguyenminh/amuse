using Amuse.Domain.Identity;

namespace Amuse.Modules.Identity.Features.Shared;

public sealed record PersonaContextRequest(
    PersonaContextType Type,
    Guid? OrgId,
    Guid? ListenerId);
