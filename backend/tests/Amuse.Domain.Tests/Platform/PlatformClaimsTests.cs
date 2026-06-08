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
        Assert.Contains(PlatformClaims.ReadAccounting, effective);
        Assert.Contains(PlatformClaims.ManageAccounting, effective);
        Assert.Contains(PlatformClaims.ManagePurchases, effective);
        Assert.Contains(PlatformClaims.ManagePayouts, effective);
    }

    [Fact]
    public void Root_can_manage_and_review_without_explicit_manage_claim_string()
    {
        Assert.True(PlatformClaims.CanManageOrganizations(["platform:root"]));
        Assert.True(PlatformClaims.CanReviewOrganizations(["platform:root"]));
        Assert.True(PlatformClaims.CanAssumeAnyOrganizationPersona(["platform:root"]));
        Assert.True(PlatformClaims.CanInstantApproveOrganizationsOnCreate(["platform:root"]));
        Assert.True(PlatformClaims.CanReadAccounting(["platform:root"]));
        Assert.True(PlatformClaims.CanManageAccounting(["platform:root"]));
        Assert.True(PlatformClaims.CanManagePurchases(["platform:root"]));
        Assert.True(PlatformClaims.CanManagePayouts(["platform:root"]));
    }

    [Fact]
    public void Review_only_operator_can_review_but_not_manage()
    {
        Assert.True(PlatformClaims.CanReviewOrganizations(["review:platform:organizations"]));
        Assert.False(PlatformClaims.CanManageOrganizations(["review:platform:organizations"]));
        Assert.False(PlatformClaims.CanAssumeAnyOrganizationPersona(["review:platform:organizations"]));
        Assert.False(PlatformClaims.CanReadAccounting(["review:platform:organizations"]));
    }

    [Fact]
    public void Manage_operator_can_manage_and_review()
    {
        Assert.True(PlatformClaims.CanManageOrganizations(["manage:platform:organizations"]));
        Assert.True(PlatformClaims.CanReviewOrganizations(["manage:platform:organizations"]));
        Assert.True(PlatformClaims.CanAssumeAnyOrganizationPersona(["manage:platform:organizations"]));
    }

    [Fact]
    public void Accounting_read_claim_does_not_imply_manage()
    {
        Assert.True(PlatformClaims.CanReadAccounting(["read:platform:accounting:all"]));
        Assert.False(PlatformClaims.CanManageAccounting(["read:platform:accounting:all"]));
    }

    [Fact]
    public void Manage_all_expands_accounting_and_payout_claims()
    {
        var effective = PlatformClaims.ExpandEffectiveClaims(["manage:platform:all"], isRootOperator: false);

        Assert.Contains(PlatformClaims.ReadAccounting, effective);
        Assert.Contains(PlatformClaims.ManagePurchases, effective);
        Assert.Contains(PlatformClaims.ManagePayouts, effective);
    }
}
