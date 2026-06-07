using Amuse.Domain.Tenancy;
using Amuse.Modules.Tenancy.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Tenancy.Contracts;

internal sealed class TenancyOrganizationReadModel(TenancyDbContext db) : ITenancyOrganizationReadModel
{
    public async Task<OrganizationTrustTier?> GetTrustTierAsync(
        OrganizationId organizationId,
        CancellationToken cancellationToken)
    {
        var trustTier = await db.Organizations
            .AsNoTracking()
            .Where(organization => organization.Id == organizationId)
            .Select(organization => (OrganizationTrustTier?)organization.TrustTier)
            .FirstOrDefaultAsync(cancellationToken);

        return trustTier;
    }

    public Task<bool> ExistsAsync(
        OrganizationId organizationId,
        CancellationToken cancellationToken) =>
        db.Organizations
            .AsNoTracking()
            .AnyAsync(organization => organization.Id == organizationId, cancellationToken);
}
