using System.Security.Claims;
using Amuse.Domain.Tenancy;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Tenancy.Features.Shared;
using Amuse.Modules.Tenancy.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Tenancy.Features.RemoveMember;

internal sealed class RemoveMemberHandler(TenancyDbContext dbContext)
{
    public async Task<Result> HandleAsync(
        Guid organizationId,
        Guid memberId,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var accountResult = TenancyAccountAccessor.GetAccountId(principal);
        if (!accountResult.IsSuccess)
            return Result.Failure(accountResult.Error!);

        if (organizationId == Guid.Empty)
            return Result.Failure(TenancyErrors.OrganizationNotFound);

        var orgId = OrganizationId.From(organizationId);
        var member = await dbContext.OrganizationMembers
            .FirstOrDefaultAsync(
                m => m.Id == memberId && m.OrganizationId == orgId && m.Status == MembershipStatus.Active,
                cancellationToken);
        if (member is null)
            return Result.Failure(TenancyErrors.MemberNotFound);

        var actor = await dbContext.OrganizationMembers
            .FirstOrDefaultAsync(
                m => m.OrganizationId == orgId
                     && m.AccountId == accountResult.Value!
                     && m.Status == MembershipStatus.Active,
                cancellationToken);

        var policy = OwnershipPolicy.ValidateCanRemoveMember(member, actor);
        if (!policy.IsSuccess)
            return policy;

        var remove = member.MarkRemoved();
        if (!remove.IsSuccess)
            return remove;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
