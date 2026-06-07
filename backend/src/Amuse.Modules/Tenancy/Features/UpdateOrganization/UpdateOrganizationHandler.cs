using System.Security.Claims;
using Amuse.Domain.SharedKernel;
using Amuse.Domain.Tenancy;
using Amuse.Modules.Common.Time;
using Amuse.Modules.Tenancy.Features.Common;
using Amuse.Modules.Tenancy.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Tenancy.Features.UpdateOrganization;

internal sealed class UpdateOrganizationHandler(
    TenancyDbContext dbContext,
    IClock clock,
    TenancyAuditWriter auditWriter)
{
    public async Task<Result<OrganizationResponse>> HandleAsync(
        Guid organizationId,
        UpdateOrganizationProfileRequest request,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var accountResult = TenancyAccountAccessor.GetAccountId(principal);
        if (!accountResult.IsSuccess)
            return Result<OrganizationResponse>.Failure(accountResult.Error!);

        if (organizationId == Guid.Empty)
            return Result<OrganizationResponse>.Failure(TenancyErrors.OrganizationNotFound);

        var orgId = OrganizationId.From(organizationId);
        var accountId = accountResult.Value!;

        var row = await (
            from member in dbContext.OrganizationMembers
            join org in dbContext.Organizations
                on member.OrganizationId equals org.Id
            where member.AccountId == accountId
                  && member.OrganizationId == orgId
                  && member.Status == MembershipStatus.Active
                  && org.LifecycleStatus != OrganizationLifecycleStatus.Closed
            select new { Organization = org, member.IsOwner }
        ).FirstOrDefaultAsync(cancellationToken);

        if (row is null)
            return Result<OrganizationResponse>.Failure(TenancyErrors.NotOrganizationMember);

        var organization = row.Organization;
        var before = TenancyAuditSnapshotMapper.FromOrganization(organization);
        var updateResult = organization.UpdateProfile(
            request.Description,
            request.WebsiteUrl,
            request.CountryCode,
            request.ImprintName,
            clock.UtcNow);

        if (!updateResult.IsSuccess)
            return Result<OrganizationResponse>.Failure(updateResult.Error!);

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditWriter.WriteUpdateAsync(
            TenancyAuditTables.Organization,
            organization.Id.Value,
            before,
            TenancyAuditSnapshotMapper.FromOrganization(organization),
            accountId.Value,
            cancellationToken);

        return Result<OrganizationResponse>.Success(
            OrganizationDtoMapper.ToResponse(organization, row.IsOwner));
    }
}
