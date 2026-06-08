using System.Security.Claims;
using Amuse.Modules.Catalog.Features.Common;

namespace Amuse.Modules.Catalog.Tests;

public sealed class CatalogClaimGuardTests
{
    [Fact]
    public void Artist_scoped_read_implies_child_release_and_group_access()
    {
        var artistId = Guid.CreateVersion7();
        var releaseId = Guid.CreateVersion7();
        var groupId = Guid.CreateVersion7();
        var principal = PrincipalWithClaims($"read:catalog:artist:{artistId:D}");

        Assert.True(CatalogClaimGuard.CanRead(
            principal,
            new CatalogReadContext("release", releaseId, ArtistId: artistId)));
        Assert.True(CatalogClaimGuard.CanRead(
            principal,
            new CatalogReadContext("release_group", groupId, ArtistId: artistId)));
        Assert.False(CatalogClaimGuard.CanRead(
            principal,
            new CatalogReadContext("release", releaseId, ArtistId: Guid.CreateVersion7())));
    }

    [Fact]
    public void Release_group_scoped_read_implies_member_releases()
    {
        var artistId = Guid.CreateVersion7();
        var groupId = Guid.CreateVersion7();
        var releaseId = Guid.CreateVersion7();
        var principal = PrincipalWithClaims($"read:catalog:release_group:{groupId:D}");

        Assert.True(CatalogClaimGuard.CanRead(
            principal,
            new CatalogReadContext(
                "release",
                releaseId,
                ArtistId: artistId,
                ReleaseGroupId: groupId)));
        Assert.False(CatalogClaimGuard.CanRead(
            principal,
            new CatalogReadContext(
                "release",
                releaseId,
                ArtistId: artistId,
                ReleaseGroupId: Guid.CreateVersion7())));
    }

    private static ClaimsPrincipal PrincipalWithClaims(params string[] claims)
    {
        var identity = new ClaimsIdentity();
        foreach (var claim in claims)
            identity.AddClaim(new Claim("claims", claim));

        identity.AddClaim(new Claim("ctx", "org"));
        identity.AddClaim(new Claim("org_id", Guid.CreateVersion7().ToString()));
        return new ClaimsPrincipal(identity);
    }
}
