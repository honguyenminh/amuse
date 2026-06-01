using Amuse.Api.IntegrationTests;
using Amuse.Domain.Catalog;
using Amuse.Domain.Tenancy;
using Amuse.Modules.Catalog.Features.Shared;
using Amuse.Modules.Catalog.Persistence;
using Amuse.Modules.Identity.Features.Shared;
using Amuse.Modules.Tenancy.Features.Shared;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Amuse.Api.IntegrationTests;

[Collection(AmuseApiCollection.Name)]
public sealed class CatalogArtistSlugTests(AmuseApiFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    [Fact]
    public async Task Create_artist_rejects_duplicate_slug()
    {
        using var client = fixture.CreateClient();
        await AuthenticateIndieOrgAsync(client, "catalog-slug-dup@amuse.test");

        var first = await client.PostAsJsonAsync(
            "/api/v1/catalog/artists",
            new { name = "First Artist", slug = "slug-flow-test", bio = (string?)null },
            JsonOptions);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await client.PostAsJsonAsync(
            "/api/v1/catalog/artists",
            new { name = "Second Artist", slug = "slug-flow-test", bio = (string?)null },
            JsonOptions);
        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);

        var problem = await second.Content.ReadFromJsonAsync<ProblemDetailsDto>(JsonOptions);
        Assert.NotNull(problem);
        Assert.Equal("catalog.duplicate_slug", problem.Title);
    }

    [Fact]
    public async Task Slug_availability_reports_taken_and_available()
    {
        using var client = fixture.CreateClient();
        await AuthenticateIndieOrgAsync(client, "catalog-slug-avail@amuse.test");

        await using (var scope = fixture.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
            var artist = Artist.Create(
                ArtistId.New(),
                "Existing Artist",
                Slug.From("existing-slug"),
                DateTimeOffset.UtcNow).Value!;
            db.Artists.Add(artist);
            await db.SaveChangesAsync();
        }

        var taken = await client.GetFromJsonAsync<ArtistSlugAvailabilityResponse>(
            "/api/v1/catalog/manage/artists/slug-availability?slug=existing-slug",
            JsonOptions);
        Assert.NotNull(taken);
        Assert.True(taken.IsValid);
        Assert.False(taken.IsAvailable);
        Assert.Equal("existing-slug", taken.NormalizedSlug);

        var available = await client.GetFromJsonAsync<ArtistSlugAvailabilityResponse>(
            "/api/v1/catalog/manage/artists/slug-availability?slug=Fresh New Name",
            JsonOptions);
        Assert.NotNull(available);
        Assert.True(available.IsValid);
        Assert.True(available.IsAvailable);
        Assert.Equal("fresh-new-name", available.NormalizedSlug);
    }

    private async Task AuthenticateIndieOrgAsync(HttpClient client, string email)
    {
        const string password = "Password1234!";
        await RegisterAndConfirmAsync(client, email, password);
        var accountTokens = await LoginAccountTokensAsync(client, email, password);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accountTokens.AccessToken);

        var createOrg = await client.PostAsJsonAsync(
            "/api/v1/tenancy/organizations",
            new
            {
                displayName = "Slug Test Org",
                orgClass = OrganizationClass.IndieGroup,
                createDefaultArtist = false,
            },
            JsonOptions);
        createOrg.EnsureSuccessStatusCode();
        var org = await createOrg.Content.ReadFromJsonAsync<OrganizationResponse>(JsonOptions);

        var orgTokens = await RefreshOrgPersonaAsync(
            client,
            accountTokens.RefreshToken!,
            org!.Id);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", orgTokens.AccessToken);
    }

    private async Task RegisterAndConfirmAsync(HttpClient client, string email, string password)
    {
        fixture.CaptureEmailSender.Reset();
        var register = await client.PostAsJsonAsync(
            "/api/v1/identity/register/password",
            new { email, password, portal = "business" },
            JsonOptions);
        Assert.Equal(HttpStatusCode.Accepted, register.StatusCode);

        var confirmUri = new Uri(fixture.CaptureEmailSender.LastConfirmUrl!);
        var query = QueryHelpers.ParseQuery(confirmUri.Query);
        var confirm = await client.PostAsJsonAsync(
            "/api/v1/identity/confirm-email",
            new
            {
                userId = Guid.Parse(query["userId"]!),
                token = Uri.UnescapeDataString(query["token"]!),
            },
            JsonOptions);
        Assert.Equal(HttpStatusCode.NoContent, confirm.StatusCode);
    }

    private static async Task<AuthTokenResponse> LoginAccountTokensAsync(
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

    private sealed record ProblemDetailsDto(string? Title, string? Detail);
}
