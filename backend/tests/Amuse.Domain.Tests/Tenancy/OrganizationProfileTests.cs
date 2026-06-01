using Amuse.Domain.Identity;
using Amuse.Domain.Tenancy;

namespace Amuse.Domain.Tests.Tenancy;

public sealed class OrganizationProfileTests
{
    private static readonly AccountId Creator = AccountId.From(Guid.CreateVersion7());
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-06-01T00:00:00+00:00");

    [Fact]
    public void UpdateProfile_updates_metadata_fields()
    {
        var org = Organization.RegisterIndieGroup("Indie Band", Creator, Now).Value!;

        var result = org.UpdateProfile(
            "New description",
            "https://example.com",
            "us",
            "Indie Imprint",
            Now);

        Assert.True(result.IsSuccess);
        Assert.Equal("New description", org.Description);
        Assert.Equal("https://example.com", org.WebsiteUrl);
        Assert.Equal("US", org.CountryCode);
        Assert.Equal("Indie Imprint", org.ImprintName);
    }

    [Fact]
    public void UpdateProfile_rejects_invalid_website_url()
    {
        var org = Organization.RegisterIndieGroup("Indie Band", Creator, Now).Value!;

        var result = org.UpdateProfile(null, "not-a-url", null, null, Now);

        Assert.False(result.IsSuccess);
        Assert.Equal(TenancyErrors.InvalidOrganizationWebsiteUrl.Code, result.Error!.Code);
    }

    [Fact]
    public void UpdateProfile_rejects_when_closed()
    {
        var org = Organization.RegisterIndieGroup("Indie Band", Creator, Now).Value!;
        Assert.True(org.Close(Now).IsSuccess);

        var result = org.UpdateProfile("Bio", null, null, null, Now);

        Assert.False(result.IsSuccess);
        Assert.Equal(TenancyErrors.OrganizationClosed.Code, result.Error!.Code);
    }
}
