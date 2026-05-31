using Amuse.Domain.Tenancy;

namespace Amuse.Domain.Tests.Tenancy;

public sealed class OrgClaimTests
{
    [Theory]
    [InlineData("read:org:all", true)]
    [InlineData("manage:membership:all", true)]
    [InlineData("manage:member_permissions:all", true)]
    [InlineData("read:catalog:artist:0194a7b2-c3d4-7890-abcd-ef1234567890", true)]
    [InlineData("org:read", false)]
    [InlineData("read:org", false)]
    [InlineData("read:unknown:all", false)]
    [InlineData("review:platform:organizations", true)]
    public void TryParse_validates_format(string value, bool expected)
    {
        Assert.Equal(expected, OrgClaim.TryParse(value, out _));
    }

    [Fact]
    public void Manage_membership_all_does_not_imply_member_permissions()
    {
        var granted = new HashSet<string>(StringComparer.Ordinal) { "manage:membership:all" };
        Assert.False(OrgClaim.Matches(OrgClaim.MemberPermissionsClaim, granted));
    }

    [Fact]
    public void Matches_scope_wide_grant()
    {
        var granted = new HashSet<string>(StringComparer.Ordinal) { "read:catalog:all" };
        Assert.True(OrgClaim.Matches("read:catalog:artist:0194a7b2-c3d4-7890-abcd-ef1234567890", granted));
        Assert.False(OrgClaim.Matches("manage:catalog:all", granted));
    }

    [Fact]
    public void Migrate_legacy_claim_strings()
    {
        Assert.True(OrgClaim.TryMigrateLegacy("catalog:upload", out var migrated));
        Assert.Equal("upload:catalog:all", migrated);
    }

    [Fact]
    public void NormalizeClaims_migrates_legacy_values()
    {
        var normalized = OrgClaim.NormalizeClaims(["org:read", "membership:manage"]);
        Assert.Contains("read:org:all", normalized);
        Assert.Contains("manage:membership:all", normalized);
    }
}
