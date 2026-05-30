using Amuse.Domain.Identity;

namespace Amuse.Modules.Tenancy.Contracts;

/// <summary>
/// Resolves off-platform contact details for organization creators (platform review workflow).
/// Implemented via Identity persistence; contract lives in Tenancy as B2B tenancy concern.
/// </summary>
public interface IOrganizationCreatorContactLookup
{
    Task<IReadOnlyDictionary<Guid, OrganizationApplicationOwner>> GetByAccountIdsAsync(
        IReadOnlyCollection<AccountId> accountIds,
        CancellationToken cancellationToken);
}
