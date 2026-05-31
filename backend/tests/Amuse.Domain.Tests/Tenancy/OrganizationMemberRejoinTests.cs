using Amuse.Domain.Identity;
using Amuse.Domain.Tenancy;

namespace Amuse.Domain.Tests.Tenancy;

public sealed class OrganizationMemberRejoinTests
{
    private static readonly AccountId Account = AccountId.From(Guid.CreateVersion7());
    private static readonly OrganizationId OrgId = OrganizationId.From(Guid.CreateVersion7());
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-05-31T12:00:00+00:00");

    [Fact]
    public void RejoinFromInvite_reactivates_removed_member_with_invite_claims()
    {
        var org = Amuse.Domain.Tenancy.Organization.RegisterIndieGroup("Band", Account, Now).Value!;
        var capabilities = org.EvaluateCapabilities();
        var member = OrganizationMember.CreateFromInvite(
            OrgId,
            Account,
            OrgClaimPresets.ViewerPresetLabel,
            OrgClaimPresets.Viewer).Value!;
        Assert.True(member.MarkRemoved().IsSuccess);

        var rejoin = member.RejoinFromInvite(
            OrgClaimPresets.MemberManagerPresetLabel,
            OrgClaimPresets.MemberManager,
            capabilities);

        Assert.True(rejoin.IsSuccess);
        Assert.Equal(MembershipStatus.Active, member.Status);
        Assert.Equal(OrgClaimPresets.MemberManagerPresetLabel, member.PresetRoleLabel);
        Assert.Contains("manage:membership:all", member.Claims);
        Assert.False(member.IsOwner);
    }

    [Fact]
    public void RejoinFromInvite_fails_when_member_is_still_active()
    {
        var org = Amuse.Domain.Tenancy.Organization.RegisterIndieGroup("Band", Account, Now).Value!;
        var member = OrganizationMember.CreateFromInvite(
            OrgId,
            Account,
            OrgClaimPresets.ViewerPresetLabel,
            OrgClaimPresets.Viewer).Value!;

        var rejoin = member.RejoinFromInvite(
            OrgClaimPresets.ViewerPresetLabel,
            OrgClaimPresets.Viewer,
            org.EvaluateCapabilities());

        Assert.False(rejoin.IsSuccess);
        Assert.Equal(TenancyErrors.DuplicateMember.Code, rejoin.Error!.Code);
    }
}
