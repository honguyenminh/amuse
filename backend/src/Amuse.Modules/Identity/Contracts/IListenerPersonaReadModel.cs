using Amuse.Domain.Identity;
using Amuse.Domain.Listener;
using Amuse.Domain.SharedKernel;

namespace Amuse.Modules.Identity.Contracts;

public interface IListenerPersonaReadModel
{
    Task<Result<PersonaAccessContext>> GetListenerContextAsync(
        AccountId accountId,
        ListenerProfileId listenerId,
        CancellationToken cancellationToken);

    Task<ListenerProfileId?> GetProfileIdForAccountAsync(
        AccountId accountId,
        CancellationToken cancellationToken);

    Task<Result<ListenerProfileId>> EnsureProfileForAccountAsync(
        AccountId accountId,
        CancellationToken cancellationToken);
}
