using Amuse.Domain.Identity;
using Amuse.Domain.Listener;
using Amuse.Domain.SharedKernel;
using Amuse.Domain.Tenancy;
using Amuse.Modules.Identity.Contracts;

namespace Amuse.Modules.Identity.Auth;

internal static class PersonaAccess
{
    public static Task<Result<PersonaAccessContext>> ResolveAsync(
        ITenancyPersonaReadModel tenancyReadModel,
        IListenerPersonaReadModel listenerReadModel,
        IPlatformPersonaReadModel platformReadModel,
        AccountId accountId,
        PersonaContext context,
        CancellationToken cancellationToken) =>
        context.Type switch
        {
            PersonaContextType.Org => tenancyReadModel.GetOrgContextAsync(
                accountId,
                OrganizationId.From(context.OrgId!.Value),
                cancellationToken),
            PersonaContextType.Listener => listenerReadModel.GetListenerContextAsync(
                accountId,
                ListenerProfileId.From(context.ListenerId!.Value),
                cancellationToken),
            PersonaContextType.Platform => platformReadModel.GetPlatformContextAsync(accountId, cancellationToken),
            _ => Task.FromResult(Result<PersonaAccessContext>.Failure(IdentityErrors.InvalidPersonaContext)),
        };
}
