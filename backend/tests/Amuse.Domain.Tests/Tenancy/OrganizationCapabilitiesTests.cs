using Amuse.Domain.Identity;
using Amuse.Domain.Platform;
using Amuse.Domain.Tenancy;

namespace Amuse.Domain.Tests.Tenancy;

public sealed class OrganizationCapabilitiesTests
{
    private static readonly AccountId Creator = AccountId.From(Guid.CreateVersion7());
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-05-27T00:00:00+00:00");

    [Fact]
    public void Indie_group_active_allows_upload_but_not_publish_public()
    {
        var org = Organization.RegisterIndieGroup("Indie Band", Creator, Now).Value!;
        var capabilities = org.EvaluateCapabilities();

        Assert.True(capabilities.CanUpload);
        Assert.True(capabilities.CanWriteDraft);
        Assert.False(capabilities.CanPublishPublic);
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
        Assert.DoesNotContain("catalog:upload", capabilities.ToClaimStrings());
    }

    [Fact]
    public void Closed_org_is_detected()
    {
        var org = Organization.RegisterIndieGroup("Indie Band", Creator, Now).Value!;
        Assert.True(org.Close(Now).IsSuccess);
        Assert.True(org.IsClosed);
    }

    [Fact]
    public void FilterClaimsForCapabilities_removes_member_writes_when_suspended()
    {
        var org = Organization.RegisterIndieGroup("Indie Band", Creator, Now).Value!;
        Assert.True(org.Suspend(Now).IsSuccess);

        var filtered = OrgCapabilities.FilterClaimsForCapabilities(
            OrgClaimPresets.OwnerAdmin,
            org.EvaluateCapabilities());

        Assert.Contains("org:read", filtered);
        Assert.DoesNotContain("catalog:upload", filtered);
    }
}
