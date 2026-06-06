using Amuse.Domain.Identity;
using Amuse.Domain.Tenancy;

namespace Amuse.Domain.Tests.Tenancy;

public sealed class BusinessPortalProfileTests
{
    private static readonly AccountId Account = AccountId.From(Guid.CreateVersion7());
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-06-06T00:00:00+00:00");

    [Fact]
    public void UpdatePresentation_sets_display_name()
    {
        var profile = BusinessPortalProfile.Create(Account, Now);

        var result = profile.UpdatePresentation("Business User", 5, Now);

        Assert.True(result.IsSuccess);
        Assert.Equal("Business User", profile.DisplayName);
        Assert.True(profile.IsComplete);
    }

    [Fact]
    public void UpdatePresentation_rejects_invalid_display_name()
    {
        var profile = BusinessPortalProfile.Create(Account, Now);

        var result = profile.UpdatePresentation("", null, Now);

        Assert.False(result.IsSuccess);
        Assert.Equal(TenancyErrors.InvalidPortalProfileDisplayName.Code, result.Error!.Code);
    }
}
