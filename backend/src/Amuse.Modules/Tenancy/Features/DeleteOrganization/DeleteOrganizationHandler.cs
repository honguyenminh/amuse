using System.Security.Claims;
using Amuse.Domain.SharedKernel;
using Amuse.Domain.Tenancy;
using Amuse.Modules.Common.Time;
using Amuse.Modules.Tenancy.Features.Common;
using Amuse.Modules.Tenancy.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Tenancy.Features.DeleteOrganization;

internal sealed class DeleteOrganizationHandler(TenancyDbContext dbContext, IClock clock)
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

        var owner = await dbContext.OrganizationMembers
            .FirstOrDefaultAsync(
                m => m.OrganizationId == orgId
                     && m.AccountId == accountResult.Value!
                     && m.IsOwner
                     && m.Status == MembershipStatus.Active,
                cancellationToken);
        if (owner is null)
            return Result.Failure(TenancyErrors.NotOrganizationOwner);

        var close = organization.Close(clock.UtcNow);
        if (!close.IsSuccess)
            return close;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
