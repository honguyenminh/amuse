using Amuse.Domain.Identity;
using Amuse.Domain.SharedKernel;

namespace Amuse.Domain.Tenancy;

public sealed class OrganizationMember
{
    public Guid Id { get; private set; }
    public OrganizationId OrganizationId { get; private set; }
    public AccountId AccountId { get; private set; }
    public MembershipStatus Status { get; private set; }
    public string? PresetRoleLabel { get; private set; }
    public IReadOnlyList<string> Claims { get; private set; } = [];
    public bool IsOwner { get; private set; }

    private OrganizationMember()
    {
    }

    public bool IsActive => Status == MembershipStatus.Active;

    public static OrganizationMember CreateOwner(
        OrganizationId organizationId,
        AccountId accountId,
        string presetRoleLabel,
        IReadOnlyList<string> claims) =>
        CreateMember(organizationId, accountId, presetRoleLabel, claims, isOwner: true);

    public static Result<OrganizationMember> CreateFromInvite(
        OrganizationId organizationId,
        AccountId accountId,
        string? presetRoleLabel,
        IReadOnlyList<string> claims)
    {
        if (!OrgClaimPresets.TryResolveClaims(presetRoleLabel, claims, out var resolved))
            return Result<OrganizationMember>.Failure(TenancyErrors.InvalidClaim);

        return Result<OrganizationMember>.Success(
            CreateMember(organizationId, accountId, presetRoleLabel, resolved, isOwner: false));
    }

    public Result UpdateClaims(string? presetRoleLabel, IReadOnlyList<string> claims, OrgCapabilities capabilities)
    {
        if (!IsActive)
            return Result.Failure(TenancyErrors.InvalidMembershipTransition);

        if (!OrgClaimPresets.TryResolveClaims(presetRoleLabel, claims, out var resolved))
            return Result.Failure(TenancyErrors.InvalidClaim);

        var assignable = OrgCapabilities.FilterAssignableClaims(resolved, capabilities);
        if (assignable.Count != resolved.Count)
            return Result.Failure(TenancyErrors.ClaimNotAllowedForOrganization);

        var policy = OwnershipPolicy.ValidateCanUpdateMemberClaims(this, assignable);
        if (!policy.IsSuccess)
            return policy;

        PresetRoleLabel = string.IsNullOrWhiteSpace(presetRoleLabel) ? null : presetRoleLabel.Trim();
        Claims = assignable;
        return Result.Success();
    }

    public Result MarkRemoved()
    {
        if (!IsActive)
            return Result.Failure(TenancyErrors.InvalidMembershipTransition);

        if (IsOwner)
            return Result.Failure(TenancyErrors.CannotModifyOwner);

        Status = MembershipStatus.Removed;
        return Result.Success();
    }

    public Result RejoinFromInvite(
        string? presetRoleLabel,
        IReadOnlyList<string> claims,
        OrgCapabilities capabilities)
    {
        if (IsActive)
            return Result.Failure(TenancyErrors.DuplicateMember);

        if (Status != MembershipStatus.Removed)
            return Result.Failure(TenancyErrors.InvalidMembershipTransition);

        if (IsOwner)
            return Result.Failure(TenancyErrors.InvalidMembershipTransition);

        if (!OrgClaimPresets.TryResolveClaims(presetRoleLabel, claims, out var resolved))
            return Result.Failure(TenancyErrors.InvalidClaim);

        var assignable = OrgCapabilities.FilterAssignableClaims(resolved, capabilities);
        if (assignable.Count != resolved.Count)
            return Result.Failure(TenancyErrors.ClaimNotAllowedForOrganization);

        Status = MembershipStatus.Active;
        PresetRoleLabel = string.IsNullOrWhiteSpace(presetRoleLabel) ? null : presetRoleLabel.Trim();
        Claims = assignable;
        IsOwner = false;
        return Result.Success();
    }

    public Result TransferOwnershipFrom(OrganizationMember currentOwner)
    {
        var policy = OwnershipPolicy.ValidateTransferOwnership(currentOwner, this);
        if (!policy.IsSuccess)
            return policy;

        currentOwner.IsOwner = false;
        IsOwner = true;

        if (!OrgClaim.ContainsAdminEquivalent(Claims))
        {
            PresetRoleLabel = OrgClaimPresets.OwnerPresetLabel;
            Claims = OrgClaimPresets.OwnerAdmin;
        }

        return Result.Success();
    }

    public Result ForceOwnershipFrom(OrganizationMember? currentOwner)
    {
        var policy = OwnershipPolicy.ValidateForceTransfer(this);
        if (!policy.IsSuccess)
            return policy;

        if (currentOwner is not null && currentOwner.IsOwner)
            currentOwner.IsOwner = false;

        IsOwner = true;
        PresetRoleLabel = OrgClaimPresets.OwnerPresetLabel;
        Claims = OrgClaimPresets.OwnerAdmin;
        return Result.Success();
    }

    private static OrganizationMember CreateMember(
        OrganizationId organizationId,
        AccountId accountId,
        string? presetRoleLabel,
        IReadOnlyList<string> claims,
        bool isOwner)
    {
        var normalizedClaims = OrgClaim.NormalizeClaims(claims);

        return new OrganizationMember
        {
            Id = Guid.CreateVersion7(),
            OrganizationId = organizationId,
            AccountId = accountId,
            Status = MembershipStatus.Active,
            PresetRoleLabel = string.IsNullOrWhiteSpace(presetRoleLabel) ? null : presetRoleLabel.Trim(),
            Claims = normalizedClaims,
            IsOwner = isOwner,
        };
    }
}
