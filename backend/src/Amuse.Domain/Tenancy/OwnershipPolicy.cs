using Amuse.Domain.Identity;
using Amuse.Domain.SharedKernel;

namespace Amuse.Domain.Tenancy;

public static class OwnershipPolicy
{
    public static Result ValidateCanRemoveMember(OrganizationMember target, OrganizationMember? actor)
    {
        if (target.IsOwner)
            return Result.Failure(TenancyErrors.CannotModifyOwner);

        if (actor is not null && actor.Id == target.Id)
            return Result.Failure(TenancyErrors.CannotRemoveSelf);

        return Result.Success();
    }

    public static Result ValidateCanUpdateMemberClaims(OrganizationMember target, IReadOnlyList<string> newClaims)
    {
        if (!target.IsOwner)
            return Result.Success();

        if (!OrgClaim.ContainsAdminEquivalent(newClaims))
            return Result.Failure(TenancyErrors.CannotDemoteOwner);

        return Result.Success();
    }

    public static Result ValidateTransferOwnership(OrganizationMember currentOwner, OrganizationMember target)
    {
        if (!currentOwner.IsOwner)
            return Result.Failure(TenancyErrors.NotOrganizationOwner);

        if (!currentOwner.IsActive || !target.IsActive)
            return Result.Failure(TenancyErrors.InvalidMembershipTransition);

        if (target.IsOwner)
            return Result.Failure(TenancyErrors.CannotTransferOwnershipToOwner);

        return Result.Success();
    }

    public static Result ValidateForceTransfer(OrganizationMember target)
    {
        if (!target.IsActive)
            return Result.Failure(TenancyErrors.InvalidMembershipTransition);

        return Result.Success();
    }
}
