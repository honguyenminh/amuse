using Amuse.Domain.Identity;

namespace Amuse.Modules.Listener.Contracts;

public interface IListenerPreferenceReadModel
{
    Task<bool?> GetAllowUnverifiedArtistsAsync(
        AccountId accountId,
        CancellationToken cancellationToken);
}
