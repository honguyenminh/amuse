using Amuse.Modules.Catalog.Features.Shared;

namespace Amuse.Modules.Catalog.Tests;

public sealed class CatalogSlugHelperTests
{
    [Fact]
    public void TryParseReleaseSlug_accepts_bitter_etude()
    {
        var result = CatalogSlugHelper.TryParseReleaseSlug("bitter-etude");
        Assert.True(result.IsSuccess);
        Assert.Equal("bitter-etude", result.Value!.Value);
    }

    [Theory]
    [InlineData("---", "bitter-etude", "bitter-etude")]
    [InlineData("♪♪♪", "bitter-etude", "bitter-etude")]
    [InlineData("Valid Title", "bitter-etude", "bitter-etude")]
    [InlineData("Valid Title", null, "valid-title")]
    public void ResolveReleaseGroupSlugBase_prefers_requested_release_slug(
        string title,
        string? requestedReleaseSlug,
        string expected)
    {
        var result = CatalogSlugHelper.ResolveReleaseGroupSlugBase(title, requestedReleaseSlug);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value!.Value);
    }

    [Fact]
    public void ResolveReleaseGroupSlugBase_fails_when_title_unslugifiable_and_no_release_slug()
    {
        var result = CatalogSlugHelper.ResolveReleaseGroupSlugBase("---", null);
        Assert.False(result.IsSuccess);
        Assert.Equal("catalog.invalid_slug", result.Error!.Code);
    }

    [Fact]
    public void ResolveReleaseGroupSlugBase_does_not_fall_back_to_title_when_release_slug_invalid()
    {
        var result = CatalogSlugHelper.ResolveReleaseGroupSlugBase("Valid Title", "---");
        Assert.False(result.IsSuccess);
        Assert.Equal("catalog.invalid_slug", result.Error!.Code);
    }
}
