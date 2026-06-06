using Amuse.Domain.Identity;
using Amuse.Domain.Platform;
using Amuse.Domain.Tenancy;

namespace Amuse.Domain.Tests.Tenancy;

public sealed class OrganizationCapabilitiesTests
{
    private static readonly AccountId Creator = AccountId.From(Guid.CreateVersion7());
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-05-27T00:00:00+00:00");

    [Fact]
    public void Indie_group_active_allows_upload_and_publish_to_discover()
    {
        var org = Organization.RegisterIndieGroup("Indie Band", Creator, Now).Value!;
        var capabilities = org.EvaluateCapabilities();

        Assert.True(capabilities.CanUpload);
        Assert.True(capabilities.CanWriteDraft);
        Assert.True(capabilities.CanPublishPublic);
        Assert.False(capabilities.CanReadPayout);
    }

    [Fact]
    public void Backing_pending_allows_org_read_only()
    {
        var org = Organization.RegisterBackingOrg("Big Label", Creator, Now).Value!;
        var capabilities = org.EvaluateCapabilities();

        Assert.True(capabilities.CanReadOrg);
        Assert.True(capabilities.CanReadMembership);
        Assert.False(capabilities.CanUpload);
        Assert.False(capabilities.CanPublishPublic);
    }

    [Fact]
    public void Backing_approved_allows_publish_and_payout_read()
    {
        var org = Organization.RegisterBackingOrg("Big Label", Creator, Now).Value!;
        Assert.True(org.Approve(PlatformOperatorId.Root, Now).IsSuccess);

        var capabilities = org.EvaluateCapabilities();

        Assert.True(capabilities.CanPublishPublic);
        Assert.True(capabilities.CanReadPayout);
    }

    [Fact]
    public void Suspended_strips_write_capabilities()
    {
        var org = Organization.RegisterIndieGroup("Indie Band", Creator, Now).Value!;
        Assert.True(org.Suspend(Now).IsSuccess);

        var capabilities = org.EvaluateCapabilities();

        Assert.True(capabilities.CanReadOrg);
        Assert.False(capabilities.CanUpload);
        Assert.DoesNotContain("upload:catalog:all", capabilities.ToClaimStrings());
    }

    [Fact]
    public void Closed_org_is_detected()
    {
        var org = Organization.RegisterIndieGroup("Indie Band", Creator, Now).Value!;
        Assert.True(org.Close(Now).IsSuccess);
        Assert.True(org.IsClosed);
    }

    [Fact]
    public void Closed_org_can_be_recovered_to_active()
    {
        var org = Organization.RegisterIndieGroup("Indie Band", Creator, Now).Value!;
        Assert.True(org.Close(Now).IsSuccess);
        Assert.True(org.RecoverFromClosed(Now).IsSuccess);
        Assert.False(org.IsClosed);
        Assert.Equal(OrganizationLifecycleStatus.Active, org.LifecycleStatus);
    }

    [Fact]
    public void RecoverFromClosed_rejects_non_closed_org()
    {
        var org = Organization.RegisterIndieGroup("Indie Band", Creator, Now).Value!;
        var result = org.RecoverFromClosed(Now);
        Assert.False(result.IsSuccess);
        Assert.Equal(TenancyErrors.InvalidLifecycleTransition, result.Error);
    }

    /// <summary>
    /// Regression guard: every claim referenced by presets must map in
    /// <see cref="OrgCapabilities.FilterAssignableClaims"/> when all member-facing capabilities are on.
    /// The admin invite bug was caused by <c>manage:member_permissions:all</c> missing from that mapping.
    /// </summary>
    [Fact]
    public void Every_preset_claim_is_assignable_when_all_member_capabilities_enabled()
    {
        var fullCapabilities = new OrgCapabilities(
            CanReadOrg: true,
            CanReadMembership: true,
            CanUpload: true,
            CanWriteDraft: true,
            CanPublishPublic: true,
            CanReadPayout: true);

        var presetClaims = OrgClaimPresets.AllDefinitions
            .SelectMany(p => p.Claims)
            .Distinct(StringComparer.Ordinal);

        foreach (var claim in presetClaims)
        {
            var assignable = OrgCapabilities.FilterAssignableClaims([claim], fullCapabilities);
            Assert.True(
                assignable.Contains(claim),
                $"Preset claim '{claim}' is not assignable even when all member capabilities are enabled.");
        }
    }

    [Theory]
    [InlineData(OrgClaimPresets.OwnerPresetLabel)]
    [InlineData(OrgClaimPresets.MemberManagerPresetLabel)]
    [InlineData(OrgClaimPresets.CatalogEditorPresetLabel)]
    [InlineData(OrgClaimPresets.ViewerPresetLabel)]
    public void Every_preset_is_fully_assignable_for_active_indie_group(string presetLabel)
    {
        var org = Organization.RegisterIndieGroup("Indie Band", Creator, Now).Value!;
        var preset = OrgClaimPresets.AllDefinitions.Single(p =>
            string.Equals(p.Label, presetLabel, StringComparison.Ordinal));

        var assignable = OrgCapabilities.FilterAssignableClaims(
            preset.Claims,
            org.EvaluateCapabilities());

        Assert.Equal(preset.Claims.Count, assignable.Count);
    }

    [Theory]
    [InlineData(OrgClaimPresets.MemberManagerPresetLabel)]
    [InlineData(OrgClaimPresets.ViewerPresetLabel)]
    public void Membership_presets_are_fully_assignable_for_pending_backing_org(string presetLabel)
    {
        var org = Organization.RegisterBackingOrg("Big Label", Creator, Now).Value!;
        var preset = OrgClaimPresets.AllDefinitions.Single(p =>
            string.Equals(p.Label, presetLabel, StringComparison.Ordinal));

        var assignable = OrgCapabilities.FilterAssignableClaims(
            preset.Claims,
            org.EvaluateCapabilities());

        Assert.Equal(preset.Claims.Count, assignable.Count);
    }

    [Fact]
    public void FilterAssignableClaims_allows_full_admin_preset_for_active_indie_group()
    {
        var org = Organization.RegisterIndieGroup("Indie Band", Creator, Now).Value!;
        var assignable = OrgCapabilities.FilterAssignableClaims(
            OrgClaimPresets.OwnerAdmin,
            org.EvaluateCapabilities());

        Assert.Equal(OrgClaimPresets.OwnerAdmin.Count, assignable.Count);
        Assert.Contains(OrgClaim.MemberPermissionsClaim, assignable);
    }

    [Fact]
    public void FilterAssignableClaims_rejects_admin_catalog_writes_for_pending_backing_org()
    {
        var org = Organization.RegisterBackingOrg("Big Label", Creator, Now).Value!;
        var assignable = OrgCapabilities.FilterAssignableClaims(
            OrgClaimPresets.OwnerAdmin,
            org.EvaluateCapabilities());

        Assert.DoesNotContain("upload:catalog:all", assignable);
        Assert.DoesNotContain("write_draft:catalog:all", assignable);
        Assert.True(assignable.Count < OrgClaimPresets.OwnerAdmin.Count);
    }

    [Fact]
    public void FilterClaimsForCapabilities_removes_member_writes_when_suspended()
    {
        var org = Organization.RegisterIndieGroup("Indie Band", Creator, Now).Value!;
        Assert.True(org.Suspend(Now).IsSuccess);

        var filtered = OrgCapabilities.FilterClaimsForCapabilities(
            OrgClaimPresets.OwnerAdmin,
            org.EvaluateCapabilities());

        Assert.Contains("read:org:all", filtered);
        Assert.DoesNotContain("upload:catalog:all", filtered);
    }
}
