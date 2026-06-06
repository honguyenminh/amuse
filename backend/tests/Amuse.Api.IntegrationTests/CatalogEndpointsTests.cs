using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Amuse.Modules.Catalog.Persistence;
using Amuse.Modules.Identity.Features.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Amuse.Api.IntegrationTests;

[Collection(AmuseApiCollection.Name)]
public sealed class CatalogEndpointsTests(AmuseApiFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task Home_returns_recent_releases_and_artists()
    {
        using var client = fixture.CreateClient();

        var response = await client.GetAsync("/api/v1/catalog/home");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.True(payload.GetProperty("recentReleases").GetArrayLength() > 0);
        Assert.True(payload.GetProperty("featuredArtists").GetArrayLength() > 0);
    }

    [Fact]
    public async Task Release_detail_returns_tracks_for_seeded_id()
    {
        using var client = fixture.CreateClient();

        var home = await client.GetFromJsonAsync<JsonElement>("/api/v1/catalog/home", JsonOptions);
        var releaseId = home.GetProperty("recentReleases")[0].GetProperty("id").GetString();

        var response = await client.GetAsync($"/api/v1/catalog/releases/{releaseId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal(releaseId, payload.GetProperty("id").GetString());
        Assert.True(payload.GetProperty("tracks").GetArrayLength() >= 1);
    }

    [Fact]
    public async Task Artist_detail_returns_discography_for_seeded_id()
    {
        using var client = fixture.CreateClient();

        var home = await client.GetFromJsonAsync<JsonElement>("/api/v1/catalog/home", JsonOptions);
        var artistId = home.GetProperty("recentReleases")[0].GetProperty("artistId").GetString();

        var response = await client.GetAsync($"/api/v1/catalog/artists/{artistId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal(artistId, payload.GetProperty("id").GetString());
        Assert.True(payload.GetProperty("releases").GetArrayLength() >= 1);
    }

    [Fact]
    public async Task Release_detail_unknown_id_returns_problem()
    {
        using var client = fixture.CreateClient();

        var response = await client.GetAsync("/api/v1/catalog/releases/00000000-0000-0000-0000-000000000099");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal("catalog.release_not_found", problem.GetProperty("title").GetString());
    }

    [Fact]
    public async Task Catalog_home_is_public()
    {
        using var client = fixture.CreateClient();
        var response = await client.GetAsync("/api/v1/catalog/home");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Stream_info_returns_track_stream_not_ready_until_ingested()
    {
        using var client = fixture.CreateClient();
        await AuthorizeAsync(client);

        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var trackId = await db.Tracks
            .AsNoTracking()
            .Where(t => t.AudioMasterKey != null && t.AudioStreamKey == null)
            .Select(t => t.Id.Value)
            .FirstOrDefaultAsync();

        Assert.NotEqual(Guid.Empty, trackId);

        var response = await client.GetAsync($"/api/v1/catalog/tracks/{trackId}/stream-info");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal("catalog.track_stream_not_ready", problem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Stream_info_requires_authentication()
    {
        using var client = fixture.CreateClient();
        var response = await client.GetAsync(
            "/api/v1/catalog/tracks/00000000-0000-0000-0000-000000000099/stream-info");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Stream_info_unknown_track_returns_problem()
    {
        using var client = fixture.CreateClient();
        await AuthorizeAsync(client);

        var response = await client.GetAsync(
            "/api/v1/catalog/tracks/00000000-0000-0000-0000-000000000099/stream-info");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal("catalog.track_not_found", problem.GetProperty("title").GetString());
    }

    private static async Task AuthorizeAsync(HttpClient client)
    {
        var login = new HttpRequestMessage(HttpMethod.Post, "/api/v1/identity/login/password")
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

        using var response = await client.SendAsync(login);
        response.EnsureSuccessStatusCode();
        var tokens = (await response.Content.ReadFromJsonAsync<AuthTokenResponse>(JsonOptions))!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
    }
}
