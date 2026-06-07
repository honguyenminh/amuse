using System.Security.Claims;
using Amuse.Domain.SharedKernel;
using Amuse.Domain.Tenancy;
using Amuse.Modules.Common.Time;
using Amuse.Modules.Tenancy.Contracts;
using Amuse.Modules.Tenancy.Features.Shared;
using Amuse.Modules.Tenancy.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Tenancy.Features.AcceptInvite;

internal sealed class AcceptInviteHandler(
    TenancyDbContext dbContext,
    IClock clock,
    IAccountEmailLookup accountEmailLookup)
{
    public async Task<Result<AcceptInviteResponse>> HandleAsync(
        string token,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var accountResult = TenancyAccountAccessor.GetAccountId(principal);
        if (!accountResult.IsSuccess)
            return Result<AcceptInviteResponse>.Failure(accountResult.Error!);

        if (string.IsNullOrWhiteSpace(token))
            return Result<AcceptInviteResponse>.Failure(TenancyErrors.InviteNotFound);

        var tokenHash = OrganizationInvite.HashToken(token);
        var invite = await dbContext.OrganizationInvites
            .FirstOrDefaultAsync(i => i.TokenHash == tokenHash, cancellationToken);
        if (invite is null)
            return Result<AcceptInviteResponse>.Failure(TenancyErrors.InviteNotFound);

        var organization = await dbContext.Organizations
            .FirstOrDefaultAsync(o => o.Id == invite.OrganizationId, cancellationToken);
        if (organization is null || organization.IsClosed)
            return Result<AcceptInviteResponse>.Failure(TenancyErrors.OrganizationNotFound);

        var accountEmail = await accountEmailLookup.GetEmailAsync(accountResult.Value!, cancellationToken);
        if (accountEmail is null)
            return Result<AcceptInviteResponse>.Failure(TenancyErrors.InviteEmailMismatch);

        var existingMember = await dbContext.OrganizationMembers
            .FirstOrDefaultAsync(
                m => m.OrganizationId == invite.OrganizationId
                     && m.AccountId == accountResult.Value!,
                cancellationToken);

        var now = clock.UtcNow;
        var acceptance = OrganizationInviteAcceptance.Apply(
            invite,
            organization,
            accountResult.Value!,
            accountEmail,
            existingMember,
            now);
        if (!acceptance.IsSuccess)
            return Result<AcceptInviteResponse>.Failure(acceptance.Error!);

        var member = acceptance.Value!;
        if (existingMember is null)
            dbContext.OrganizationMembers.Add(member);

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<AcceptInviteResponse>.Success(new AcceptInviteResponse(
            invite.OrganizationId.Value,
            member.Id));
    }
}
