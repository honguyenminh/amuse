using Amuse.Domain.Platform;

namespace Amuse.Domain.Tests.Platform;

public sealed class PlatformClaimsTests
{
    [Fact]
    public void Root_expands_to_manage_and_review_claims_for_token_mint()
    {
        var effective = PlatformClaims.ExpandEffectiveClaims([], isRootOperator: true);

        Assert.Contains(PlatformClaims.Root, effective);
        Assert.Contains(PlatformClaims.ManageOrganizations, effective);
        Assert.Contains(PlatformClaims.ReviewOrganizations, effective);
        Assert.Contains(PlatformClaims.ManageAll, effective);
    }

    [Fact]
    public void Root_can_manage_and_review_without_explicit_manage_claim_string()
    {
        Assert.True(PlatformClaims.CanManageOrganizations(["platform:root"]));
        Assert.True(PlatformClaims.CanReviewOrganizations(["platform:root"]));
        Assert.True(PlatformClaims.CanAssumeAnyOrganizationPersona(["platform:root"]));
        Assert.True(PlatformClaims.CanInstantApproveOrganizationsOnCreate(["platform:root"]));
    }

    [Fact]
    public void Review_only_operator_can_review_but_not_manage()
    {
        Assert.True(PlatformClaims.CanReviewOrganizations(["review:platform:organizations"]));
        Assert.False(PlatformClaims.CanManageOrganizations(["review:platform:organizations"]));
        Assert.False(PlatformClaims.CanAssumeAnyOrganizationPersona(["review:platform:organizations"]));
    }

    [Fact]
    public void Manage_operator_can_manage_and_review()
    {
        Assert.True(PlatformClaims.CanManageOrganizations(["manage:platform:organizations"]));
        Assert.True(PlatformClaims.CanReviewOrganizations(["manage:platform:organizations"]));
        Assert.True(PlatformClaims.CanAssumeAnyOrganizationPersona(["manage:platform:organizations"]));
    }
}
