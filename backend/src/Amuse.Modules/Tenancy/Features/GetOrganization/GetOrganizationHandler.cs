using System.Security.Claims;
using Amuse.Domain.SharedKernel;
using Amuse.Domain.Tenancy;
using Amuse.Modules.Tenancy.Features.Common;
using Amuse.Modules.Tenancy.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Tenancy.Features.GetOrganization;

internal sealed class GetOrganizationHandler(TenancyDbContext dbContext)
{
    public async Task<Result<OrganizationResponse>> HandleAsync(
        Guid organizationId,
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
            from member in dbContext.OrganizationMembers.AsNoTracking()
            join organization in dbContext.Organizations.AsNoTracking()
                on member.OrganizationId equals organization.Id
            where member.AccountId == accountId
                  && member.OrganizationId == orgId
                  && member.Status == MembershipStatus.Active
                  && organization.LifecycleStatus != OrganizationLifecycleStatus.Closed
            select new { organization, member.IsOwner }
        ).FirstOrDefaultAsync(cancellationToken);

        if (row is null)
            return Result<OrganizationResponse>.Failure(TenancyErrors.NotOrganizationMember);

        return Result<OrganizationResponse>.Success(
            OrganizationDtoMapper.ToResponse(row.organization, row.IsOwner));
    }
}
