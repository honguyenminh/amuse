using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Amuse.Domain.Catalog;
using Amuse.Domain.Tenancy;
using Amuse.Modules.Catalog.Features.Common;
using Amuse.Modules.Catalog.Features.Common;
using Amuse.Modules.Identity.Features.Common;
using Amuse.Modules.Tenancy.Features.Common;

namespace Amuse.Api.IntegrationTests;

[Collection(AmuseApiCollection.Name)]
public sealed class CatalogArtistVisibilityTests(AmuseApiFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    [Fact]
    public async Task Create_artist_in_platform_verified_org_is_platform_verified()
    {
        using var client = fixture.CreateClient();
        var platformTokens = await LoginPlatformTokensAsync(client);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", platformTokens.AccessToken);

        var createOrg = await client.PostAsJsonAsync(
            "/api/v1/tenancy/organizations",
            new { displayName = "Verified Label", orgClass = OrganizationClass.BackingOrg },
            JsonOptions);
        Assert.Equal(HttpStatusCode.Created, createOrg.StatusCode);
        var org = await createOrg.Content.ReadFromJsonAsync<OrganizationResponse>(JsonOptions);
        Assert.NotNull(org);
        Assert.Equal("approved", org.OnboardingStatus);

        var orgTokens = await RefreshOrgPersonaAsync(client, platformTokens.RefreshToken!, org.Id);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", orgTokens.AccessToken);

        var createArtist = await client.PostAsJsonAsync(
            "/api/v1/catalog/artists",
            new { name = "Verified Artist", slug = "verified-artist", bio = (string?)null },
            JsonOptions);
        Assert.Equal(HttpStatusCode.Created, createArtist.StatusCode);

        var artist = await createArtist.Content.ReadFromJsonAsync<ManageArtistSummaryResponse>(JsonOptions);
        Assert.NotNull(artist);
        Assert.Equal(ArtistVisibilityTier.PlatformVerified, artist.VisibilityTier);
    }

    [Fact]
    public async Task Create_artist_in_indie_org_stays_unverified()
    {
        using var client = fixture.CreateClient();
        var platformTokens = await LoginPlatformTokensAsync(client);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", platformTokens.AccessToken);

        var createOrg = await client.PostAsJsonAsync(
            "/api/v1/tenancy/organizations",
            new
            {
                displayName = "Indie Band Org",
                orgClass = OrganizationClass.IndieGroup,
                createDefaultArtist = false,
            },
            JsonOptions);
        Assert.Equal(HttpStatusCode.Created, createOrg.StatusCode);
        var org = await createOrg.Content.ReadFromJsonAsync<OrganizationResponse>(JsonOptions);
        Assert.NotNull(org);

        var orgTokens = await RefreshOrgPersonaAsync(client, platformTokens.RefreshToken!, org.Id);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", orgTokens.AccessToken);

        var createArtist = await client.PostAsJsonAsync(
            "/api/v1/catalog/artists",
            new { name = "Indie Artist", slug = "indie-artist", bio = (string?)null },
            JsonOptions);
        Assert.Equal(HttpStatusCode.Created, createArtist.StatusCode);

        var artist = await createArtist.Content.ReadFromJsonAsync<ManageArtistSummaryResponse>(JsonOptions);
        Assert.NotNull(artist);
        Assert.Equal(ArtistVisibilityTier.Unverified, artist.VisibilityTier);
    }

    private static async Task<AuthTokenResponse> LoginPlatformTokensAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync(
            "/api/v1/identity/login/password",
            new
            {
                email = "root@amuse.local",
                password = "ChangeMe_Root123!",
                context = new { type = "platform", orgId = (Guid?)null, listenerId = (Guid?)null },
            },
            JsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthTokenResponse>(JsonOptions))!;
    }

    private static async Task<AuthTokenResponse> RefreshOrgPersonaAsync(
        HttpClient client,
        string refreshToken,
        Guid orgId)
    {
        var response = await client.PostAsJsonAsync(
            "/api/v1/identity/refresh",
            new
            {
                refreshToken,
                context = new { type = "org", orgId, listenerId = (Guid?)null },
            },
            JsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthTokenResponse>(JsonOptions))!;
    }
}
