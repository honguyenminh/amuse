using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Amuse.Domain.Tenancy;
using Amuse.Modules.Identity.Features.Shared;
using Amuse.Modules.Tenancy.Features.Shared;

namespace Amuse.Api.IntegrationTests;

[Collection(AmuseApiCollection.Name)]
public sealed class TenancyOrganizationFlowTests(AmuseApiFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task Create_indie_org_then_refresh_org_persona_succeeds()
    {
        using var client = fixture.CreateClient();
        var accountTokens = await LoginPlatformTokensAsync(client);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accountTokens.AccessToken);

        var create = await client.PostAsJsonAsync(
            "/api/v1/tenancy/organizations",
            new { displayName = "Integration Indie", orgClass = OrganizationClass.IndieGroup },
            JsonOptions);

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var created = await create.Content.ReadFromJsonAsync<OrganizationResponse>(JsonOptions);
        Assert.NotNull(created);
        Assert.Equal("notRequired", created.OnboardingStatus);
        Assert.False(created.Capabilities.CanPublishPublic);

        var refresh = await client.PostAsJsonAsync(
            "/api/v1/identity/refresh",
            new
            {
                refreshToken = accountTokens.RefreshToken,
                context = new { type = "org", orgId = created.Id, listenerId = (Guid?)null },
            },
            JsonOptions);

        Assert.Equal(HttpStatusCode.OK, refresh.StatusCode);
        var orgTokens = await refresh.Content.ReadFromJsonAsync<AuthTokenResponse>(JsonOptions);
        Assert.NotNull(orgTokens);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", orgTokens.AccessToken);

        var profile = await client.GetAsync($"/api/v1/tenancy/organizations/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, profile.StatusCode);
    }

    [Fact]
    public async Task Platform_can_approve_backing_org_application()
    {
        using var client = fixture.CreateClient();
        var accountTokens = await LoginPlatformTokensAsync(client);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accountTokens.AccessToken);

        var create = await client.PostAsJsonAsync(
            "/api/v1/tenancy/organizations",
            new { displayName = "Integration Label", orgClass = OrganizationClass.BackingOrg },
            JsonOptions);

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var created = await create.Content.ReadFromJsonAsync<OrganizationResponse>(JsonOptions);
        Assert.NotNull(created);
        Assert.Equal("pendingReview", created.OnboardingStatus);

        var platformRefresh = await client.PostAsJsonAsync(
            "/api/v1/identity/refresh",
            new
            {
                refreshToken = accountTokens.RefreshToken,
                context = new { type = "platform", orgId = (Guid?)null, listenerId = (Guid?)null },
            },
            JsonOptions);

        var platformTokens = await platformRefresh.Content.ReadFromJsonAsync<AuthTokenResponse>(JsonOptions);
        Assert.NotNull(platformTokens);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", platformTokens.AccessToken);

        var applications = await client.GetAsync(
            "/api/v1/platform/organizations/applications?status=pendingReview");
        Assert.Equal(HttpStatusCode.OK, applications.StatusCode);
        var applicationList = await applications.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal(JsonValueKind.Array, applicationList.ValueKind);
        var match = applicationList.EnumerateArray()
            .FirstOrDefault(item =>
                item.GetProperty("organizationId").GetString() == created!.Id.ToString());
        Assert.NotEqual(default, match);
        Assert.Equal(
            "root@amuse.local",
            match.GetProperty("owner").GetProperty("email").GetString());

        var approve = await client.PostAsync(
            $"/api/v1/platform/organizations/{created.Id}/approve",
            null);
        Assert.Equal(HttpStatusCode.NoContent, approve.StatusCode);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accountTokens.AccessToken);

        var profile = await client.GetAsync($"/api/v1/tenancy/organizations/{created.Id}");
        profile.EnsureSuccessStatusCode();
        var updated = await profile.Content.ReadFromJsonAsync<OrganizationResponse>(JsonOptions);
        Assert.NotNull(updated);
        Assert.Equal("approved", updated.OnboardingStatus);
        Assert.True(updated.Capabilities.CanPublishPublic);
    }

    [Fact]
    public async Task Owner_can_soft_delete_org_and_platform_can_recover()
    {
        using var client = fixture.CreateClient();
        var accountTokens = await LoginPlatformTokensAsync(client);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accountTokens.AccessToken);

        var create = await client.PostAsJsonAsync(
            "/api/v1/tenancy/organizations",
            new { displayName = "Delete Recover Org", orgClass = OrganizationClass.IndieGroup },
            JsonOptions);

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var created = await create.Content.ReadFromJsonAsync<OrganizationResponse>(JsonOptions);
        Assert.NotNull(created);

        var orgTokens = await RefreshOrgPersonaAsync(client, accountTokens.RefreshToken!, created.Id);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", orgTokens.AccessToken);

        var delete = await client.DeleteAsync($"/api/v1/tenancy/organizations/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accountTokens.AccessToken);

        var list = await client.GetAsync("/api/v1/tenancy/organizations");
        list.EnsureSuccessStatusCode();
        var organizations = await list.Content.ReadFromJsonAsync<OrganizationResponse[]>(JsonOptions);
        Assert.NotNull(organizations);
        Assert.DoesNotContain(organizations, o => o.Id == created.Id);

        var platformRefresh = await client.PostAsJsonAsync(
            "/api/v1/identity/refresh",
            new
            {
                refreshToken = orgTokens.RefreshToken,
                context = new { type = "platform", orgId = (Guid?)null, listenerId = (Guid?)null },
            },
            JsonOptions);
        Assert.Equal(HttpStatusCode.OK, platformRefresh.StatusCode);
        var platformTokens = await platformRefresh.Content.ReadFromJsonAsync<AuthTokenResponse>(JsonOptions);
        Assert.NotNull(platformTokens);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", platformTokens.AccessToken);

        var recover = await client.PostAsync(
            $"/api/v1/platform/organizations/{created.Id}/recover",
            null);
        Assert.Equal(HttpStatusCode.NoContent, recover.StatusCode);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accountTokens.AccessToken);

        var profile = await client.GetAsync($"/api/v1/tenancy/organizations/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, profile.StatusCode);
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
}
