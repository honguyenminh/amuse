using Amuse.Domain.Billing;
using Amuse.Domain.Identity;

namespace Amuse.Modules.Billing.Contracts;

public interface IEntitlementReadModel
{
    Task<bool> OwnsTrackAsync(
        AccountId accountId,
        Guid trackId,
        Guid releaseId,
        CancellationToken cancellationToken);

    Task<bool> OwnsReleaseAsync(
        AccountId accountId,
        Guid releaseId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Purchase>> GetActivePurchasesForAccountAsync(
        AccountId accountId,
        CancellationToken cancellationToken);
}
