using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Amuse.Domain.Tenancy;
using Amuse.Modules.Identity.Features.Common;
using Amuse.Modules.Platform.Features.ListOrganizationApplications;
using Amuse.Modules.Tenancy.Features.Common;
using Amuse.Modules.Tenancy.Features.ListOrganizationAudit;
using Microsoft.AspNetCore.WebUtilities;

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
        Assert.True(created.Capabilities.CanPublishPublic);

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
    public async Task Owner_can_update_organization_profile_and_list_audit()
    {
        using var client = fixture.CreateClient();
        const string email = "org-profile@amuse.test";
        const string password = "Password1234!";

        await RegisterAndConfirmAsync(client, email, password);
        var accountTokens = await LoginListenerTokensAsync(client, email, password);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accountTokens.AccessToken);

        var create = await client.PostAsJsonAsync(
            "/api/v1/tenancy/organizations",
            new
            {
                displayName = "Profile Update Org",
                orgClass = OrganizationClass.IndieGroup,
                description = "Initial description",
            },
            JsonOptions);
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var created = await create.Content.ReadFromJsonAsync<OrganizationResponse>(JsonOptions);
        Assert.NotNull(created);

        var orgTokens = await RefreshOrgPersonaAsync(
            client,
            accountTokens.RefreshToken!,
            created.Id);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", orgTokens.AccessToken);

        var update = await client.PatchAsJsonAsync(
            $"/api/v1/tenancy/organizations/{created.Id}",
            new
            {
                description = "Updated organization description",
                websiteUrl = "https://example.com",
                countryCode = "US",
                imprintName = "Example Imprint",
            },
            JsonOptions);
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var updated = await update.Content.ReadFromJsonAsync<OrganizationResponse>(JsonOptions);
        Assert.NotNull(updated);
        Assert.Equal("Updated organization description", updated.Description);
        Assert.Equal("https://example.com", updated.WebsiteUrl);
        Assert.Equal("US", updated.CountryCode);
        Assert.Equal("Example Imprint", updated.ImprintName);

        var audit = await client.GetFromJsonAsync<TenancyAuditListResponse>(
            $"/api/v1/tenancy/organizations/{created.Id}/audit",
            JsonOptions);
        Assert.NotNull(audit);
        Assert.Contains(audit.Items, entry => entry.Action == "created");
        Assert.Contains(audit.Items, entry => entry.Action == "updated");
        Assert.Contains(
            audit.Items,
            entry => entry.Action == "updated"
                     && entry.AfterJson?.Contains("Updated organization description") == true);
    }

    [Fact]
    public async Task Platform_root_creates_backing_org_as_approved_immediately()
    {
        using var client = fixture.CreateClient();
        var accountTokens = await LoginPlatformTokensAsync(client);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accountTokens.AccessToken);

        var create = await client.PostAsJsonAsync(
            "/api/v1/tenancy/organizations",
            new { displayName = "Root Instant Label", orgClass = OrganizationClass.BackingOrg },
            JsonOptions);

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var created = await create.Content.ReadFromJsonAsync<OrganizationResponse>(JsonOptions);
        Assert.NotNull(created);
        Assert.Equal("approved", created.OnboardingStatus);
        Assert.True(created.Capabilities.CanPublishPublic);
    }

    [Fact]
    public async Task Platform_can_approve_backing_org_application()
    {
        using var client = fixture.CreateClient();
        var accountTokens = await LoginPlatformTokensAsync(client);

        const string applicantEmail = "backing-applicant@amuse.test";
        const string applicantPassword = "Password1234!";
        await RegisterAndConfirmAsync(client, applicantEmail, applicantPassword);
        var applicantTokens = await LoginListenerTokensAsync(client, applicantEmail, applicantPassword);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", applicantTokens.AccessToken);

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
            applicantEmail,
            match.GetProperty("owner").GetProperty("email").GetString());

        var approve = await client.PostAsync(
            $"/api/v1/platform/organizations/{created.Id}/approve",
            null);
        Assert.Equal(HttpStatusCode.NoContent, approve.StatusCode);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", applicantTokens.AccessToken);

        var profile = await client.GetAsync($"/api/v1/tenancy/organizations/{created.Id}");
        profile.EnsureSuccessStatusCode();
        var updated = await profile.Content.ReadFromJsonAsync<OrganizationResponse>(JsonOptions);
        Assert.NotNull(updated);
        Assert.Equal("approved", updated.OnboardingStatus);
        Assert.True(updated.Capabilities.CanPublishPublic);
    }

    [Fact]
    public async Task Platform_root_token_can_list_closed_organizations()
    {
        using var client = fixture.CreateClient();
        var platformTokens = await LoginPlatformTokensAsync(client);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", platformTokens.AccessToken);

        var closedList = await client.GetAsync("/api/v1/platform/organizations/closed");
        Assert.Equal(HttpStatusCode.OK, closedList.StatusCode);
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

        var membersAfterDelete = await client.GetAsync(
            $"/api/v1/tenancy/organizations/{created.Id}/members");
        Assert.Equal(HttpStatusCode.NotFound, membersAfterDelete.StatusCode);

        var profileAfterDelete = await client.GetAsync(
            $"/api/v1/tenancy/organizations/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, profileAfterDelete.StatusCode);

        var refreshClosedOrg = await client.PostAsJsonAsync(
            "/api/v1/identity/refresh",
            new
            {
                refreshToken = orgTokens.RefreshToken,
                context = new { type = "org", orgId = created.Id, listenerId = (Guid?)null },
            },
            JsonOptions);
        Assert.NotEqual(HttpStatusCode.OK, refreshClosedOrg.StatusCode);

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
        var platformTokensAfterDelete = await platformRefresh.Content.ReadFromJsonAsync<AuthTokenResponse>(JsonOptions);
        Assert.NotNull(platformTokensAfterDelete);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", platformTokensAfterDelete.AccessToken);

        var closedList = await client.GetAsync("/api/v1/platform/organizations/closed");
        closedList.EnsureSuccessStatusCode();
        var closedOrganizations = await closedList.Content.ReadFromJsonAsync<OrganizationApplicationResponse[]>(JsonOptions);
        Assert.NotNull(closedOrganizations);
        Assert.Contains(closedOrganizations, o => o.OrganizationId == created.Id);

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

    private async Task RegisterAndConfirmAsync(
        HttpClient client,
        string email,
        string password)
    {
        fixture.CaptureEmailSender.Reset();
        var register = await client.PostAsJsonAsync(
            "/api/v1/identity/register/password",
            new { email, password, portal = "business" },
            JsonOptions);
        Assert.Equal(HttpStatusCode.Accepted, register.StatusCode);

        var confirmUri = new Uri(fixture.CaptureEmailSender.LastConfirmUrl!);
        var confirmQuery = QueryHelpers.ParseQuery(confirmUri.Query);
        var confirm = await client.PostAsJsonAsync(
            "/api/v1/identity/confirm-email",
            new
            {
                userId = Guid.Parse(confirmQuery["userId"]!),
                token = Uri.UnescapeDataString(confirmQuery["token"]!),
            },
            JsonOptions);
        Assert.Equal(HttpStatusCode.NoContent, confirm.StatusCode);
    }

    private static async Task<AuthTokenResponse> LoginListenerTokensAsync(
        HttpClient client,
        string email,
        string password)
    {
        var response = await client.PostAsJsonAsync(
            "/api/v1/identity/login/password",
            new
            {
                email,
                password,
                context = new { type = "listener", orgId = (Guid?)null, listenerId = (Guid?)null },
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
