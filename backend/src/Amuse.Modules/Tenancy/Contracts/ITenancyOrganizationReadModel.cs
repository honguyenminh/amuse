using Amuse.Domain.Tenancy;

namespace Amuse.Modules.Tenancy.Contracts;

public interface ITenancyOrganizationReadModel
{
    Task<OrganizationTrustTier?> GetTrustTierAsync(
        OrganizationId organizationId,
        CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<Guid, OrganizationTrustTier>> GetTrustTiersAsync(
        IEnumerable<OrganizationId> organizationIds,
        CancellationToken cancellationToken);

    Task<bool> ExistsAsync(
        OrganizationId organizationId,
        CancellationToken cancellationToken);

    Task<OrganizationLifecycleStatus?> GetLifecycleStatusAsync(
        OrganizationId organizationId,
        CancellationToken cancellationToken);
}
