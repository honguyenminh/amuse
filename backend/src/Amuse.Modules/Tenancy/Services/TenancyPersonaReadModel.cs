using Amuse.Domain.Identity;
using Amuse.Domain.Platform;
using Amuse.Domain.SharedKernel;
using Amuse.Domain.Tenancy;
using Amuse.Modules.Identity.Contracts;
using Amuse.Modules.Platform.Contracts;
using Amuse.Modules.Tenancy.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Tenancy.Services;

internal sealed class TenancyPersonaReadModel(
    TenancyDbContext dbContext,
    IPlatformOperatorLookup platformOperatorLookup) : ITenancyPersonaReadModel
{
    public async Task<IReadOnlyList<OrgPersonaListing>> ListAvailableOrgsAsync(
        AccountId accountId,
        CancellationToken cancellationToken)
    {
        var memberOrgs = await (
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

        if (!await CanAssumeAnyOrganizationAsync(accountId, cancellationToken))
            return memberOrgs;

        var assumedOrgs = await dbContext.Organizations
            .AsNoTracking()
            .OrderBy(o => o.DisplayName)
            .Select(o => new OrgPersonaListing(
                o.Id.Value,
                o.DisplayName,
                null,
                o.OrgClass,
                o.LifecycleStatus,
                o.OnboardingStatus,
                o.TrustTier))
            .ToListAsync(cancellationToken);

        var memberOrgIds = memberOrgs
            .Select(o => o.OrganizationId)
            .ToHashSet();

        return memberOrgs
            .Concat(assumedOrgs.Where(o => !memberOrgIds.Contains(o.OrganizationId)))
            .ToList();
    }

    public async Task<Result<PersonaAccessContext>> GetOrgContextAsync(
        AccountId accountId,
        OrganizationId organizationId,
        CancellationToken cancellationToken)
    {
        if (await CanAssumeAnyOrganizationAsync(accountId, cancellationToken))
        {
            var assumed = await TryBuildAssumedOrgContextAsync(organizationId, cancellationToken);
            if (assumed is not null)
                return Result<PersonaAccessContext>.Success(assumed);
        }

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

    private async Task<bool> CanAssumeAnyOrganizationAsync(
        AccountId accountId,
        CancellationToken cancellationToken)
    {
        var claims = await platformOperatorLookup.GetEffectiveClaimsForAccountAsync(
            accountId,
            cancellationToken);
        return PlatformClaims.CanAssumeAnyOrganizationPersona(claims);
    }

    private async Task<PersonaAccessContext?> TryBuildAssumedOrgContextAsync(
        OrganizationId organizationId,
        CancellationToken cancellationToken)
    {
        var organization = await dbContext.Organizations
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == organizationId, cancellationToken);

        if (organization is null)
            return null;

        var capabilities = organization.EvaluateCapabilities();
        var effectiveClaims = OrgCapabilities.FilterClaimsForCapabilities(
            OrgClaimPresets.OwnerAdmin,
            capabilities);

        return new PersonaAccessContext(
            "org",
            organization.Id.Value,
            null,
            OrgClaimPresets.OwnerPresetLabel,
            effectiveClaims.ToList());
    }
}
