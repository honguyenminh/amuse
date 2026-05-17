using Amuse.Domain.Identity;
using Amuse.Domain.SharedKernel;
using Amuse.Domain.Tenancy;
using Amuse.Modules.Identity.Contracts;
using Amuse.Modules.Tenancy.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Tenancy.Services;

internal sealed class TenancyPersonaReadModel(TenancyDbContext dbContext) : ITenancyPersonaReadModel
{
    public async Task<IReadOnlyList<OrgPersonaListing>> ListAvailableOrgsAsync(
        AccountId accountId,
        CancellationToken cancellationToken)
    {
        var memberships = await dbContext.OrganizationMembers
            .AsNoTracking()
            .Where(m => m.AccountId == accountId && m.Status == MembershipStatus.Active)
            .ToListAsync(cancellationToken);

        return memberships
            .Select(m => new OrgPersonaListing(m.OrganizationId.Value, m.PresetRoleLabel ?? string.Empty))
            .ToList();
    }

    public async Task<Result<PersonaAccessContext>> GetOrgContextAsync(
        AccountId accountId,
        OrganizationId organizationId,
        CancellationToken cancellationToken)
    {
        var member = await dbContext.OrganizationMembers
            .AsNoTracking()
            .FirstOrDefaultAsync(
                m => m.AccountId == accountId && m.OrganizationId == organizationId,
                cancellationToken);

        if (member is null || !member.IsActive)
            return Result<PersonaAccessContext>.Failure(IdentityErrors.InvalidPersonaContext);

        return Result<PersonaAccessContext>.Success(new PersonaAccessContext(
            "org",
            member.OrganizationId.Value,
            null,
            member.PresetRoleLabel,
            member.Claims.ToList()));
    }
}
