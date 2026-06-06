using Amuse.Domain.Identity;

namespace Amuse.Modules.Listener.Contracts;

public interface IListenerOnboardingStatusReadModel
{
    Task<bool> IsOnboardingCompleteAsync(AccountId accountId, CancellationToken cancellationToken);
}
