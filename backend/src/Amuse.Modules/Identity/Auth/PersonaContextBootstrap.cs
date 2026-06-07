using Amuse.Domain.Identity;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Identity.Contracts;
using Amuse.Modules.Identity.Features.Common;

namespace Amuse.Modules.Identity.Auth;

internal static class PersonaContextBootstrap
{
    public static async Task<Result<PersonaContext>> ResolveAsync(
        PersonaContextRequest request,
        AccountId accountId,
        IListenerPersonaReadModel listenerReadModel,
        CancellationToken cancellationToken)
    {
        if (request.Type == PersonaContextType.Listener && request.ListenerId is null)
        {
            var listenerId = await listenerReadModel.GetProfileIdForAccountAsync(accountId, cancellationToken);
            if (listenerId is null)
            {
                var ensured = await listenerReadModel.EnsureProfileForAccountAsync(accountId, cancellationToken);
                if (!ensured.IsSuccess)
                    return Result<PersonaContext>.Failure(ensured.Error!);

                listenerId = ensured.Value;
            }

            return Result<PersonaContext>.Success(PersonaContext.ForListener(listenerId!.Value.Value));
        }

        return PersonaContextMapper.ToDomain(request);
    }
}
