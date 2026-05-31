using Amuse.Domain.Platform;

namespace Amuse.Domain.Tests.Platform;

public sealed class PlatformClaimsTests
{
    [Fact]
    public void Root_can_assume_any_organization_persona()
    {
        Assert.True(PlatformClaims.CanAssumeAnyOrganizationPersona(["platform:root"]));
    }

    [Fact]
    public void Review_only_operator_cannot_assume_organization_persona()
    {
        Assert.False(PlatformClaims.CanAssumeAnyOrganizationPersona(["review:platform:organizations"]));
    }
}
