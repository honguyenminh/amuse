using Amuse.Domain.SharedKernel;
using Amuse.Domain.Tenancy;

namespace Amuse.Modules.Catalog.Contracts;

public interface ICatalogOrganizationBootstrap
{
    Task<Result<Guid>> CreateDefaultArtistAsync(
        OrganizationId organizationId,
        string displayName,
        OrganizationTrustTier organizationTrustTier,
        DateTimeOffset now,
        CancellationToken cancellationToken);
}
