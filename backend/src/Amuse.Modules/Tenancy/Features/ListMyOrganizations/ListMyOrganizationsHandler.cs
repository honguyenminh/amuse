using System.Security.Claims;
using Amuse.Domain.SharedKernel;
using Amuse.Domain.Tenancy;
using Amuse.Modules.Tenancy.Features.Shared;
using Amuse.Modules.Tenancy.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Tenancy.Features.ListMyOrganizations;

internal sealed class ListMyOrganizationsHandler(TenancyDbContext dbContext)
{
    public async Task<Result<IReadOnlyList<OrganizationResponse>>> HandleAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var accountResult = TenancyAccountAccessor.GetAccountId(principal);
        if (!accountResult.IsSuccess)
            return Result<IReadOnlyList<OrganizationResponse>>.Failure(accountResult.Error!);

        var accountId = accountResult.Value!;

        var rows = await (
            from member in dbContext.OrganizationMembers.AsNoTracking()
            join organization in dbContext.Organizations.AsNoTracking()
                on member.OrganizationId equals organization.Id
            where member.AccountId == accountId
                  && member.Status == MembershipStatus.Active
                  && organization.LifecycleStatus != OrganizationLifecycleStatus.Closed
            select new { organization, member.IsOwner }
        ).ToListAsync(cancellationToken);

        var responses = rows
            .Select(row => OrganizationDtoMapper.ToResponse(row.organization, row.IsOwner))
            .ToList();

        return Result<IReadOnlyList<OrganizationResponse>>.Success(responses);
    }
}
