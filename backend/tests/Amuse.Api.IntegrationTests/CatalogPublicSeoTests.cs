using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Amuse.Domain.Catalog;
using Amuse.Modules.Catalog.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Amuse.Api.IntegrationTests;

[Collection(AmuseApiCollection.Name)]
public sealed class CatalogPublicSeoTests(AmuseApiFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task Release_group_by_slugs_returns_published_editions()
    {
        const string artistSlug = "aurora-lights";
        const string groupSlug = "dawn-anatomy";

        await using (var scope = fixture.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
            var release = await db.Releases
                .FirstAsync(r => r.Slug == Slug.From("dawn-anatomy"));

            if (release.ReleaseGroupId is null)
            {
                var group = ReleaseGroup.Create(
                    ReleaseGroupId.New(),
                    release.OrganizationId,
                    release.ArtistId,
                    "Dawn Anatomy",
                    Slug.From(groupSlug),
                    release.CreatedAt).Value!;
                db.ReleaseGroups.Add(group);
                await db.SaveChangesAsync();

                await db.Releases
                    .Where(r => r.Id == release.Id)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(
                        r => r.ReleaseGroupId,
                        group.Id));
            }
        }

        using var client = fixture.CreateClient();

        var response = await client.GetAsync(
            $"/api/v1/catalog/artists/{artistSlug}/release-groups/{groupSlug}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal(groupSlug, payload.GetProperty("slug").GetString());
        Assert.Equal(artistSlug, payload.GetProperty("artistSlug").GetString());
        Assert.True(payload.GetProperty("releases").GetArrayLength() >= 1);
    }

    [Fact]
    public async Task Release_group_by_slugs_unknown_returns_problem()
    {
        using var client = fixture.CreateClient();

        var response = await client.GetAsync(
            "/api/v1/catalog/artists/aurora-lights/release-groups/does-not-exist");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal("catalog.release_group_not_found", problem.GetProperty("title").GetString());
    }

    [Fact]
    public async Task Sitemap_lists_published_catalog_entries()
    {
        using var client = fixture.CreateClient();

        var response = await client.GetAsync("/api/v1/catalog/sitemap?pageSize=100");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var entries = payload.GetProperty("entries");
        Assert.True(entries.GetArrayLength() > 0);

        var hasArtist = entries.EnumerateArray().Any(entry =>
            entry.TryGetProperty("type", out var type) && type.GetString() == "artist");
        var hasRelease = entries.EnumerateArray().Any(entry =>
            entry.TryGetProperty("type", out var type) && type.GetString() == "release");

        Assert.True(hasArtist);
        Assert.True(hasRelease);
    }

    [Fact]
    public async Task Sitemap_pagination_returns_next_cursor_when_full()
    {
        using var client = fixture.CreateClient();

        var response = await client.GetAsync("/api/v1/catalog/sitemap?pageSize=1");
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal(1, payload.GetProperty("entries").GetArrayLength());

        var nextCursor = payload.GetProperty("nextCursor").GetString();
        Assert.False(string.IsNullOrWhiteSpace(nextCursor));

        var nextPage = await client.GetAsync(
            $"/api/v1/catalog/sitemap?pageSize=1&cursor={Uri.EscapeDataString(nextCursor!)}");
        nextPage.EnsureSuccessStatusCode();

        var nextPayload = await nextPage.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal(1, nextPayload.GetProperty("entries").GetArrayLength());
    }
}
