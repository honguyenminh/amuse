using Amuse.Domain.Identity;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Identity.Features.Shared;

namespace Amuse.Modules.Identity.Auth;

internal static class PersonaContextMapper
{
    public static Result<PersonaContext> ToDomain(PersonaContextRequest request) =>
        request.Type switch
        {
            PersonaContextType.Org when request.OrgId is { } orgId && orgId != Guid.Empty =>
                Result<PersonaContext>.Success(PersonaContext.ForOrg(orgId)),
            PersonaContextType.Listener when request.ListenerId is { } listenerId && listenerId != Guid.Empty =>
                Result<PersonaContext>.Success(PersonaContext.ForListener(listenerId)),
            PersonaContextType.Platform =>
                Result<PersonaContext>.Success(PersonaContext.ForPlatform()),
            _ => Result<PersonaContext>.Failure(IdentityErrors.InvalidPersonaContext),
        };
}
