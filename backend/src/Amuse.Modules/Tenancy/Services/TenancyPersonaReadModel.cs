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
        return await (
            from member in dbContext.OrganizationMembers.AsNoTracking()
            join organization in dbContext.Organizations.AsNoTracking()
                on member.OrganizationId equals organization.Id
            where member.AccountId == accountId
                  && member.Status == MembershipStatus.Active
                  && organization.LifecycleStatus != OrganizationLifecycleStatus.Closed
            orderby organization.DisplayName
            select new OrgPersonaListing(
                member.OrganizationId.Value,
                organization.DisplayName,
                member.PresetRoleLabel,
                organization.OrgClass,
                organization.LifecycleStatus,
                organization.OnboardingStatus,
                organization.TrustTier)
        ).ToListAsync(cancellationToken);
    }

    public async Task<Result<PersonaAccessContext>> GetOrgContextAsync(
        AccountId accountId,
        OrganizationId organizationId,
        CancellationToken cancellationToken)
    {
        var row = await (
            from member in dbContext.OrganizationMembers.AsNoTracking()
            join organization in dbContext.Organizations.AsNoTracking()
                on member.OrganizationId equals organization.Id
            where member.AccountId == accountId
                  && member.OrganizationId == organizationId
            select new { member, organization }
        ).FirstOrDefaultAsync(cancellationToken);

        if (row is null || !row.member.IsActive || row.organization.IsClosed)
            return Result<PersonaAccessContext>.Failure(IdentityErrors.InvalidPersonaContext);

        var capabilities = row.organization.EvaluateCapabilities();
        var effectiveClaims = OrgCapabilities.FilterClaimsForCapabilities(
            row.member.Claims,
            capabilities);

        return Result<PersonaAccessContext>.Success(new PersonaAccessContext(
            "org",
            row.member.OrganizationId.Value,
            null,
            row.member.PresetRoleLabel,
            effectiveClaims.ToList()));
    }
}
