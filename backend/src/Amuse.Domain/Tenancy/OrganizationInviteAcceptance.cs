using Amuse.Domain.Identity;
using Amuse.Domain.SharedKernel;

namespace Amuse.Domain.Tenancy;

public static class OrganizationInviteAcceptance
{
    public static Result<OrganizationMember> Apply(
        OrganizationInvite invite,
        Organization organization,
        AccountId accountId,
        string accountEmail,
        OrganizationMember? existingMember,
        DateTimeOffset now)
    {
        if (organization.IsClosed)
            return Result<OrganizationMember>.Failure(TenancyErrors.OrganizationNotFound);

        var accept = invite.Accept(accountId, accountEmail, now);
        if (!accept.IsSuccess)
            return Result<OrganizationMember>.Failure(accept.Error!);

        if (existingMember is not null)
        {
            var rejoin = existingMember.RejoinFromInvite(
                invite.PresetRoleLabel,
                invite.Claims,
                organization.EvaluateCapabilities());
            if (!rejoin.IsSuccess)
                return Result<OrganizationMember>.Failure(rejoin.Error!);

            return Result<OrganizationMember>.Success(existingMember);
        }

        var memberResult = OrganizationMember.CreateFromInvite(
            invite.OrganizationId,
            accountId,
            invite.PresetRoleLabel,
            invite.Claims);
        if (!memberResult.IsSuccess)
            return Result<OrganizationMember>.Failure(memberResult.Error!);

        return Result<OrganizationMember>.Success(memberResult.Value!);
    }
}
