using System.Security.Claims;
using Amuse.Domain.SharedKernel;
using Amuse.Domain.Tenancy;
using Amuse.Modules.Tenancy.Features.Shared;
using Amuse.Modules.Tenancy.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Tenancy.Features.TransferOwnership;

internal sealed class TransferOwnershipHandler(TenancyDbContext dbContext)
{
    public async Task<Result> HandleAsync(
        Guid organizationId,
        TransferOwnershipRequest request,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var accountResult = TenancyAccountAccessor.GetAccountId(principal);
        if (!accountResult.IsSuccess)
            return Result.Failure(accountResult.Error!);

        if (organizationId == Guid.Empty)
            return Result.Failure(TenancyErrors.OrganizationNotFound);

        var orgId = OrganizationId.From(organizationId);
        var owner = await dbContext.OrganizationMembers
            .FirstOrDefaultAsync(
                m => m.OrganizationId == orgId
                     && m.AccountId == accountResult.Value!
                     && m.IsOwner
                     && m.Status == MembershipStatus.Active,
                cancellationToken);
        if (owner is null)
            return Result.Failure(TenancyErrors.NotOrganizationOwner);

        var target = await dbContext.OrganizationMembers
            .FirstOrDefaultAsync(
                m => m.Id == request.TargetMemberId
                     && m.OrganizationId == orgId
                     && m.Status == MembershipStatus.Active,
                cancellationToken);
        if (target is null)
            return Result.Failure(TenancyErrors.MemberNotFound);

        var transfer = target.TransferOwnershipFrom(owner);
        if (!transfer.IsSuccess)
            return transfer;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
