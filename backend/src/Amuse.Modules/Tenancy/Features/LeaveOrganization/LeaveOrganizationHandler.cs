using System.Security.Claims;
using Amuse.Domain.Tenancy;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Tenancy.Features.Shared;
using Amuse.Modules.Tenancy.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Tenancy.Features.LeaveOrganization;

internal sealed class LeaveOrganizationHandler(TenancyDbContext dbContext)
{
    public async Task<Result> HandleAsync(
        Guid organizationId,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var accountResult = TenancyAccountAccessor.GetAccountId(principal);
        if (!accountResult.IsSuccess)
            return Result.Failure(accountResult.Error!);

        if (organizationId == Guid.Empty)
            return Result.Failure(TenancyErrors.OrganizationNotFound);

        var orgId = OrganizationId.From(organizationId);
        var organization = await dbContext.Organizations
            .FirstOrDefaultAsync(o => o.Id == orgId, cancellationToken);
        if (organization is null || organization.IsClosed)
            return Result.Failure(TenancyErrors.OrganizationNotFound);

        var member = await dbContext.OrganizationMembers
            .FirstOrDefaultAsync(
                m => m.OrganizationId == orgId
                     && m.AccountId == accountResult.Value!
                     && m.Status == MembershipStatus.Active,
                cancellationToken);
        if (member is null)
            return Result.Failure(TenancyErrors.NotOrganizationMember);

        var leave = member.Leave();
        if (!leave.IsSuccess)
            return leave;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
