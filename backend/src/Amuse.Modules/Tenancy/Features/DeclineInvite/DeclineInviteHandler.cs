using System.Security.Claims;
using Amuse.Domain.Tenancy;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Common.Time;
using Amuse.Modules.Tenancy.Contracts;
using Amuse.Modules.Tenancy.Features.Shared;
using Amuse.Modules.Tenancy.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Tenancy.Features.DeclineInvite;

internal sealed class DeclineInviteHandler(
    TenancyDbContext dbContext,
    IClock clock,
    IAccountEmailLookup accountEmailLookup)
{
    public async Task<Result> HandleAsync(
        string token,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var accountResult = TenancyAccountAccessor.GetAccountId(principal);
        if (!accountResult.IsSuccess)
            return Result.Failure(accountResult.Error!);

        if (string.IsNullOrWhiteSpace(token))
            return Result.Failure(TenancyErrors.InviteNotFound);

        var tokenHash = OrganizationInvite.HashToken(token);
        var invite = await dbContext.OrganizationInvites
            .FirstOrDefaultAsync(i => i.TokenHash == tokenHash, cancellationToken);
        if (invite is null)
            return Result.Failure(TenancyErrors.InviteNotFound);

        var accountEmail = await accountEmailLookup.GetEmailAsync(accountResult.Value!, cancellationToken);
        if (accountEmail is null)
            return Result.Failure(TenancyErrors.InviteEmailMismatch);

        var normalizedAccountEmail = OrganizationInvite.NormalizeEmail(accountEmail);
        if (normalizedAccountEmail is null
            || !string.Equals(invite.Email, normalizedAccountEmail, StringComparison.OrdinalIgnoreCase))
            return Result.Failure(TenancyErrors.InviteEmailMismatch);

        var now = clock.UtcNow;
        if (invite.Status == OrganizationInviteStatus.Pending && now >= invite.ExpiresAt)
        {
            var expire = invite.MarkExpired(now);
            if (expire.IsSuccess)
                await dbContext.SaveChangesAsync(cancellationToken);
            return Result.Failure(TenancyErrors.InviteExpired);
        }

        var decline = invite.Revoke(now);
        if (!decline.IsSuccess)
            return decline;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
