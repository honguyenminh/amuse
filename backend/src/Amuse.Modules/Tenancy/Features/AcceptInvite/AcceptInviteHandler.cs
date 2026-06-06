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

        var now = clock.UtcNow;
        var accept = invite.Accept(accountResult.Value!, accountEmail, now);
        if (!accept.IsSuccess)
            return Result<AcceptInviteResponse>.Failure(accept.Error!);

        var existingMember = await dbContext.OrganizationMembers
            .FirstOrDefaultAsync(
                m => m.OrganizationId == invite.OrganizationId
                     && m.AccountId == accountResult.Value!,
                cancellationToken);

        OrganizationMember member;
        if (existingMember is not null)
        {
            var rejoin = existingMember.RejoinFromInvite(
                invite.PresetRoleLabel,
                invite.Claims,
                organization.EvaluateCapabilities());
            if (!rejoin.IsSuccess)
                return Result<AcceptInviteResponse>.Failure(rejoin.Error!);

            member = existingMember;
        }
        else
        {
            var memberResult = OrganizationMember.CreateFromInvite(
                invite.OrganizationId,
                accountResult.Value!,
                invite.PresetRoleLabel,
                invite.Claims);
            if (!memberResult.IsSuccess)
                return Result<AcceptInviteResponse>.Failure(memberResult.Error!);

            member = memberResult.Value!;
            dbContext.OrganizationMembers.Add(member);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<AcceptInviteResponse>.Success(new AcceptInviteResponse(
            invite.OrganizationId.Value,
            member.Id));
    }
}
