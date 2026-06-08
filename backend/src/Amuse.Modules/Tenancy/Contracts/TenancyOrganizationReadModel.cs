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

    public async Task<IReadOnlyDictionary<Guid, OrganizationTrustTier>> GetTrustTiersAsync(
        IEnumerable<OrganizationId> organizationIds,
        CancellationToken cancellationToken)
    {
        var ids = organizationIds.Distinct().ToArray();
        if (ids.Length == 0)
            return new Dictionary<Guid, OrganizationTrustTier>();

        var rows = await db.Organizations
            .AsNoTracking()
            .Where(organization => ids.Contains(organization.Id))
            .Select(organization => new
            {
                organization.Id,
                organization.TrustTier,
            })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(row => row.Id.Value, row => row.TrustTier);
    }

    public Task<bool> ExistsAsync(
        OrganizationId organizationId,
        CancellationToken cancellationToken) =>
        db.Organizations
            .AsNoTracking()
            .AnyAsync(organization => organization.Id == organizationId, cancellationToken);

    public async Task<OrganizationLifecycleStatus?> GetLifecycleStatusAsync(
        OrganizationId organizationId,
        CancellationToken cancellationToken)
    {
        var lifecycleStatus = await db.Organizations
            .AsNoTracking()
            .Where(organization => organization.Id == organizationId)
            .Select(organization => (OrganizationLifecycleStatus?)organization.LifecycleStatus)
            .FirstOrDefaultAsync(cancellationToken);

        return lifecycleStatus;
    }
}
