using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Amuse.Modules.Identity.Auth;
using Amuse.Modules.Identity.Features.Common;

namespace Amuse.Api.IntegrationTests;

[Collection(AmuseApiCollection.Name)]
public sealed class IdentityAuthFlowTests(AmuseApiFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task LoginPassword_mobile_returns_tokens()
    {
        using var client = fixture.CreateClient();
        var response = await LoginPlatformAsync(client);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var tokens = await response.Content.ReadFromJsonAsync<AuthTokenResponse>(JsonOptions);
        Assert.NotNull(tokens);
        Assert.False(string.IsNullOrWhiteSpace(tokens.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(tokens.RefreshToken));
    }

    [Fact]
    public async Task GetCurrentAccount_with_access_token_succeeds()
    {
        using var client = fixture.CreateClient();
        var tokens = await LoginPlatformTokensAsync(client);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        var me = await client.GetAsync("/api/v1/identity/me");

        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
    }

    [Fact]
    public async Task ListPersonas_includes_platform()
    {
        using var client = fixture.CreateClient();
        var tokens = await LoginPlatformTokensAsync(client);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        var response = await client.GetAsync("/api/v1/identity/personas");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("platform", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Refresh_issues_new_access_token()
    {
        using var client = fixture.CreateClient();
        var tokens = await LoginPlatformTokensAsync(client);

        var refresh = await PostRefreshAsync(client, tokens.RefreshToken!);
        Assert.Equal(HttpStatusCode.OK, refresh.StatusCode);
        var refreshed = await refresh.Content.ReadFromJsonAsync<AuthTokenResponse>(JsonOptions);
        Assert.NotNull(refreshed);
        Assert.False(string.IsNullOrWhiteSpace(refreshed.AccessToken));
        Assert.NotEqual(tokens.AccessToken, refreshed.AccessToken);
    }

    [Fact]
    public async Task Revoke_blacklists_access_and_invalidates_refresh()
    {
        using var client = fixture.CreateClient();
        var tokens = await LoginPlatformTokensAsync(client);

        var revoke = new HttpRequestMessage(HttpMethod.Post, "/api/v1/identity/revoke")
        {
            Content = JsonContent.Create(new { refreshToken = tokens.RefreshToken }, options: JsonOptions),
        };
        revoke.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        var revokeResponse = await client.SendAsync(revoke);
        Assert.Equal(HttpStatusCode.NoContent, revokeResponse.StatusCode);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        var me = await client.GetAsync("/api/v1/identity/me");
        Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);
        var problem = await me.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal("identity.token_revoked", problem.GetProperty("title").GetString());

        var refresh = await PostRefreshAsync(client, tokens.RefreshToken!);
        Assert.Equal(HttpStatusCode.BadRequest, refresh.StatusCode);
    }

    [Fact]
    public async Task LoginPassword_web_uses_refresh_cookie()
    {
        using var client = fixture.CreateClientWithCookies();
        using var request = CreatePlatformLoginRequest();
        request.Headers.Add(AuthConstants.ClientTypeHeader, AuthConstants.WebClient);

        var login = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var tokens = await login.Content.ReadFromJsonAsync<AuthTokenResponse>(JsonOptions);
        Assert.NotNull(tokens);
        Assert.Null(tokens.RefreshToken);

        var refreshRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/identity/refresh")
        {
            Content = JsonContent.Create(
                new { context = new { type = "platform", orgId = (Guid?)null, listenerId = (Guid?)null } },
                options: JsonOptions),
        };
        refreshRequest.Headers.Add(AuthConstants.ClientTypeHeader, AuthConstants.WebClient);

        var refresh = await client.SendAsync(refreshRequest);
        Assert.Equal(HttpStatusCode.OK, refresh.StatusCode);
    }

    private static async Task<HttpResponseMessage> LoginPlatformAsync(HttpClient client)
    {
        var request = CreatePlatformLoginRequest();
        return await client.SendAsync(request);
    }

    private static async Task<AuthTokenResponse> LoginPlatformTokensAsync(HttpClient client)
    {
        var response = await LoginPlatformAsync(client);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthTokenResponse>(JsonOptions))!;
    }

    private static HttpRequestMessage CreatePlatformLoginRequest() =>
        new(HttpMethod.Post, "/api/v1/identity/login/password")
        {
            Content = JsonContent.Create(
                new
                {
                    email = "root@amuse.local",
                    password = "ChangeMe_Root123!",
                    context = new { type = "platform", orgId = (Guid?)null, listenerId = (Guid?)null },
                },
                options: JsonOptions),
        };

    private static Task<HttpResponseMessage> PostRefreshAsync(HttpClient client, string refreshToken) =>
        client.PostAsJsonAsync(
            "/api/v1/identity/refresh",
            new
            {
                refreshToken,
                context = new { type = "platform", orgId = (Guid?)null, listenerId = (Guid?)null },
            },
            JsonOptions);
}
