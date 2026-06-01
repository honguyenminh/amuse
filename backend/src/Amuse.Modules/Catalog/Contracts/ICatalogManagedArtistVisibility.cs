using Amuse.Domain.Tenancy;

namespace Amuse.Modules.Catalog.Contracts;

public interface ICatalogManagedArtistVisibility
{
    Task SyncManagedArtistsForOrganizationAsync(
        OrganizationId organizationId,
        OrganizationTrustTier organizationTrustTier,
        CancellationToken cancellationToken = default);
}
