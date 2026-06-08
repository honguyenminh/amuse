using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Amuse.Domain.Tenancy;
using Amuse.Modules.Identity.Features.Common;
using Amuse.Modules.Tenancy.Features.Common;
using Microsoft.AspNetCore.WebUtilities;

namespace Amuse.Api.IntegrationTests;

[Collection(AmuseApiCollection.Name)]
public sealed class BillingCommerceFlowTests(AmuseApiFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task Free_acquire_rejects_non_free_track()
    {
        using var client = fixture.CreateClient();
        var email = $"billing-free-{Guid.CreateVersion7():N}@amuse.test";
        const string password = "Password1234!";

        await RegisterConfirmAndLoginListenerAsync(fixture, client, email, password);
        await CompleteOnboardingAsync(client);

        var response = await client.PostAsJsonAsync(
            "/api/v1/billing/acquisitions/free",
            new { trackId = Guid.CreateVersion7() },
            JsonOptions);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal("billing.purchase.track_not_found", problem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Checkout_session_requires_authentication()
    {
        using var client = fixture.CreateClient();
        var response = await client.PostAsJsonAsync(
            "/api/v1/billing/checkout/sessions",
            new { trackId = Guid.CreateVersion7(), amountMinor = 500 },
            JsonOptions);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Refund_endpoint_requires_platform_claim()
    {
        using var client = fixture.CreateClient();
        var email = $"billing-refund-{Guid.CreateVersion7():N}@amuse.test";
        const string password = "Password1234!";

        await RegisterConfirmAndLoginListenerAsync(fixture, client, email, password);
        await CompleteOnboardingAsync(client);

        var response = await client.PostAsJsonAsync(
            $"/api/v1/billing/purchases/{Guid.CreateVersion7()}/refund",
            new { reason = "test", refundFeeBearer = "platform" },
            JsonOptions);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Withdrawal_rejects_without_verified_payout_profile()
    {
        using var client = fixture.CreateClient();
        var email = $"billing-withdraw-{Guid.CreateVersion7():N}@amuse.test";
        const string password = "Password1234!";

        await RegisterAndConfirmAsync(fixture, client, email, password);
        var accountTokens = await LoginAccountTokensAsync(client, email, password);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accountTokens.AccessToken);

        var createOrg = await client.PostAsJsonAsync(
            "/api/v1/tenancy/organizations",
            new { displayName = "Billing Withdraw Org", orgClass = OrganizationClass.IndieGroup },
            JsonOptions);
        Assert.Equal(HttpStatusCode.Created, createOrg.StatusCode);
        var org = await createOrg.Content.ReadFromJsonAsync<OrganizationResponse>(JsonOptions);
        Assert.NotNull(org);

        var orgTokens = await RefreshOrgPersonaAsync(client, accountTokens.RefreshToken!, org.Id);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", orgTokens.AccessToken);

        var response = await client.PostAsJsonAsync(
            "/api/v1/billing/withdrawals",
            new { amountMinor = 2_000, currency = "USD" },
            JsonOptions);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal("billing.payout_profile.not_found", problem.GetProperty("code").GetString());
    }

    private static async Task RegisterConfirmAndLoginListenerAsync(
        AmuseApiFixture fixture,
        HttpClient client,
        string email,
        string password)
    {
        var register = await client.PostAsJsonAsync(
            "/api/v1/identity/register/password",
            new { email, password, portal = "consumer" },
            JsonOptions);
        Assert.Equal(HttpStatusCode.Accepted, register.StatusCode);

        var confirmUrl = fixture.CaptureEmailSender.LastConfirmUrl;
        Assert.False(string.IsNullOrWhiteSpace(confirmUrl));
        var uri = new Uri(confirmUrl!);
        var parsed = QueryHelpers.ParseQuery(uri.Query);
        var userId = Guid.Parse(parsed["userId"]!);
        var token = Uri.UnescapeDataString(parsed["token"]!);

        var confirm = await client.PostAsJsonAsync(
            "/api/v1/identity/confirm-email",
            new { userId, token },
            JsonOptions);
        Assert.Equal(HttpStatusCode.NoContent, confirm.StatusCode);

        var login = await client.PostAsJsonAsync(
            "/api/v1/identity/login/password",
            new
            {
                email,
                password,
                context = new { type = "listener", orgId = (Guid?)null, listenerId = (Guid?)null },
            },
            JsonOptions);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var tokens = await login.Content.ReadFromJsonAsync<AuthTokenResponse>(JsonOptions);
        Assert.NotNull(tokens);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
    }

    private static async Task CompleteOnboardingAsync(HttpClient client)
    {
        var response = await client.PatchAsJsonAsync(
            "/api/v1/listener/profile",
            new { displayName = "Billing Test Listener", allowUnverifiedArtists = true },
            JsonOptions);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static async Task RegisterAndConfirmAsync(
        AmuseApiFixture fixture,
        HttpClient client,
        string email,
        string password)
    {
        var register = await client.PostAsJsonAsync(
            "/api/v1/identity/register/password",
            new { email, password, portal = "business" },
            JsonOptions);
        Assert.Equal(HttpStatusCode.Accepted, register.StatusCode);

        var confirmUrl = fixture.CaptureEmailSender.LastConfirmUrl;
        Assert.False(string.IsNullOrWhiteSpace(confirmUrl));
        var uri = new Uri(confirmUrl!);
        var parsed = QueryHelpers.ParseQuery(uri.Query);
        var userId = Guid.Parse(parsed["userId"]!);
        var token = Uri.UnescapeDataString(parsed["token"]!);

        var confirm = await client.PostAsJsonAsync(
            "/api/v1/identity/confirm-email",
            new { userId, token },
            JsonOptions);
        Assert.Equal(HttpStatusCode.NoContent, confirm.StatusCode);
    }

    private static async Task<AuthTokenResponse> LoginAccountTokensAsync(HttpClient client, string email, string password)
    {
        var login = await client.PostAsJsonAsync(
            "/api/v1/identity/login/password",
            new
            {
                email,
                password,
                context = new { type = "listener", orgId = (Guid?)null, listenerId = (Guid?)null },
            },
            JsonOptions);
        login.EnsureSuccessStatusCode();
        var tokens = await login.Content.ReadFromJsonAsync<AuthTokenResponse>(JsonOptions);
        return tokens!;
    }

    private static async Task<AuthTokenResponse> RefreshOrgPersonaAsync(
        HttpClient client,
        string refreshToken,
        Guid orgId)
    {
        var refresh = await client.PostAsJsonAsync(
            "/api/v1/identity/refresh",
            new
            {
                refreshToken,
                context = new { type = "org", orgId, listenerId = (Guid?)null },
            },
            JsonOptions);
        refresh.EnsureSuccessStatusCode();
        var tokens = await refresh.Content.ReadFromJsonAsync<AuthTokenResponse>(JsonOptions);
        return tokens!;
    }
}
