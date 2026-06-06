using Amuse.Domain.SharedKernel;
using Amuse.Domain.Tenancy;
using Amuse.Modules.Common.Time;
using Amuse.Modules.Tenancy.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Tenancy.Features.RevokeInvite;

internal sealed class RevokeInviteHandler(TenancyDbContext dbContext, IClock clock)
{
    public async Task<Result> HandleAsync(
        Guid organizationId,
        Guid inviteId,
        CancellationToken cancellationToken)
    {
        if (organizationId == Guid.Empty)
            return Result.Failure(TenancyErrors.OrganizationNotFound);

        var orgId = OrganizationId.From(organizationId);
        var invite = await dbContext.OrganizationInvites
            .FirstOrDefaultAsync(
                i => i.Id == inviteId && i.OrganizationId == orgId,
                cancellationToken);

        if (invite is null)
            return Result.Failure(TenancyErrors.InviteNotFound);

        var revoke = invite.Revoke(clock.UtcNow);
        if (!revoke.IsSuccess)
            return revoke;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
