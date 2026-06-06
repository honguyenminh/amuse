using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Amuse.Modules.Identity.Features.Shared;
using Microsoft.AspNetCore.WebUtilities;

namespace Amuse.Api.IntegrationTests;

[Collection(AmuseApiCollection.Name)]
public sealed class IdentityRegistrationFlowTests(AmuseApiFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task Register_confirm_and_login_with_listener_bootstrap_succeeds()
    {
        fixture.CaptureEmailSender.Reset();
        using var client = fixture.CreateClient();
        var email = $"user-{Guid.CreateVersion7():N}@amuse.test";
        const string password = "Password1234!";

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
        var me = await client.GetAsync("/api/v1/identity/me");
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
    }

    [Fact]
    public async Task Register_duplicate_email_returns_email_already_registered()
    {
        using var client = fixture.CreateClient();
        const string email = "root@amuse.local";
        const string password = "Password1234!";

        var response = await client.PostAsJsonAsync(
            "/api/v1/identity/register/password",
            new { email, password, portal = "business" },
            JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal("identity.email_already_registered", problem.GetProperty("title").GetString());
    }

    [Fact]
    public async Task Login_before_confirm_returns_email_not_confirmed()
    {
        fixture.CaptureEmailSender.Reset();
        using var client = fixture.CreateClient();
        var email = $"pending-{Guid.CreateVersion7():N}@amuse.test";
        const string password = "Password1234!";

        var register = await client.PostAsJsonAsync(
            "/api/v1/identity/register/password",
            new { email, password, portal = "consumer" },
            JsonOptions);
        Assert.Equal(HttpStatusCode.Accepted, register.StatusCode);

        var login = await client.PostAsJsonAsync(
            "/api/v1/identity/login/password",
            new
            {
                email,
                password,
                context = new { type = "listener", orgId = (Guid?)null, listenerId = (Guid?)null },
            },
            JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, login.StatusCode);
        var problem = await login.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal("identity.email_not_confirmed", problem.GetProperty("title").GetString());
    }
}
