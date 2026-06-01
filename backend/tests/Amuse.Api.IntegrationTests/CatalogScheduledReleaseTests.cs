using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Amuse.Domain.Catalog;
using Amuse.Domain.Tenancy;
using Amuse.Modules.Catalog.Features.Shared;
using Amuse.Modules.Catalog.Persistence;
using Amuse.Modules.Identity.Features.Shared;
using Amuse.Modules.Media;
using Amuse.Modules.Tenancy.Features.Shared;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Amuse.Api.IntegrationTests;

[Collection(AmuseApiCollection.Name)]
public sealed class CatalogScheduledReleaseTests(AmuseApiFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    [Fact]
    public async Task Schedule_release_keeps_it_private_until_published()
    {
        using var client = fixture.CreateClient();
        const string email = "catalog-schedule@amuse.test";
        const string password = "Password1234!";

        await RegisterAndConfirmAsync(client, email, password);
        var accountTokens = await LoginAccountTokensAsync(client, email, password);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accountTokens.AccessToken);

        var createOrg = await client.PostAsJsonAsync(
            "/api/v1/tenancy/organizations",
            new
            {
                displayName = "Catalog Schedule Indie",
                orgClass = OrganizationClass.IndieGroup,
                createDefaultArtist = true,
            },
            JsonOptions);

        Assert.Equal(HttpStatusCode.Created, createOrg.StatusCode);
        var org = await createOrg.Content.ReadFromJsonAsync<OrganizationResponse>(JsonOptions);
        Assert.NotNull(org);

        var orgTokens = await RefreshOrgPersonaAsync(
            client,
            accountTokens.RefreshToken!,
            org.Id);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", orgTokens.AccessToken);

        var artists = await client.GetFromJsonAsync<ManageArtistListResponse>(
            "/api/v1/catalog/manage/artists",
            JsonOptions);
        Assert.NotNull(artists);
        Assert.NotEmpty(artists.Items);
        var artistId = artists.Items[0].Id;

        var releaseDate = "2026-06-30T00:00:00Z";
        var createRelease = await client.PostAsJsonAsync(
            $"/api/v1/catalog/artists/{artistId}/releases",
            new
            {
                title = "Scheduled Single",
                releaseType = ReleaseType.Single,
                releaseDate,
                releaseGroupId = (Guid?)null,
            },
            JsonOptions);

        Assert.Equal(HttpStatusCode.Created, createRelease.StatusCode);
        var release = await createRelease.Content.ReadFromJsonAsync<ManageReleaseDetailResponse>(JsonOptions);
        Assert.NotNull(release);

        var createTrack = await client.PostAsJsonAsync(
            $"/api/v1/catalog/releases/{release.Id}/tracks",
            new
            {
                title = "Only Track",
                trackNumber = 1,
                durationMs = 180_000,
                explicitFlag = false,
            },
            JsonOptions);

        Assert.Equal(HttpStatusCode.Created, createTrack.StatusCode);
        var track = await createTrack.Content.ReadFromJsonAsync<ManageTrackResponse>(JsonOptions);
        Assert.NotNull(track);

        var masterKey = $"masters/{track.Id}/{Guid.CreateVersion7()}.wav";
        await fixture.ObjectStorage.PutAsync(
            MediaBucket.Audio,
            masterKey,
            new byte[] { 0x00, 0x01, 0x02 },
            "audio/wav",
            CancellationToken.None);

        var complete = await client.PostAsJsonAsync(
            $"/api/v1/catalog/tracks/{track.Id}/audio-master/complete",
            new { key = masterKey, durationMs = 180_000 },
            JsonOptions);
        Assert.Equal(HttpStatusCode.OK, complete.StatusCode);

        await using (var scope = fixture.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
            var entity = await db.Tracks.FirstAsync(t => t.Id == TrackId.From(track.Id));
            entity.SetAudioStream($"dash/{track.Id}/{Guid.CreateVersion7()}/manifest.mpd");
            entity.MarkReady();
            await db.SaveChangesAsync();
        }

        var schedule = await client.PostAsync(
            $"/api/v1/catalog/releases/{release.Id}/schedule",
            null);
        Assert.Equal(HttpStatusCode.OK, schedule.StatusCode);
        var scheduled = await schedule.Content.ReadFromJsonAsync<ManageReleaseDetailResponse>(JsonOptions);
        Assert.NotNull(scheduled);
        Assert.Equal(ReleaseLifecycleStatus.Scheduled, scheduled.LifecycleStatus);

        client.DefaultRequestHeaders.Remove("Authorization");
        var publicGet = await client.GetAsync($"/api/v1/catalog/releases/{release.Id}");
        Assert.Equal(HttpStatusCode.BadRequest, publicGet.StatusCode);
    }

    [Fact]
    public async Task Cancel_schedule_returns_release_to_draft()
    {
        using var client = fixture.CreateClient();
        const string email = "catalog-cancel-schedule@amuse.test";
        const string password = "Password1234!";

        await RegisterAndConfirmAsync(client, email, password);
        var accountTokens = await LoginAccountTokensAsync(client, email, password);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accountTokens.AccessToken);

        var createOrg = await client.PostAsJsonAsync(
            "/api/v1/tenancy/organizations",
            new
            {
                displayName = "Catalog Cancel Schedule Indie",
                orgClass = OrganizationClass.IndieGroup,
                createDefaultArtist = true,
            },
            JsonOptions);

        var org = await createOrg.Content.ReadFromJsonAsync<OrganizationResponse>(JsonOptions);
        Assert.NotNull(org);

        var orgTokens = await RefreshOrgPersonaAsync(client, accountTokens.RefreshToken!, org.Id);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", orgTokens.AccessToken);

        var artists = await client.GetFromJsonAsync<ManageArtistListResponse>(
            "/api/v1/catalog/manage/artists",
            JsonOptions);
        var artistId = artists!.Items[0].Id;

        var createRelease = await client.PostAsJsonAsync(
            $"/api/v1/catalog/artists/{artistId}/releases",
            new
            {
                title = "Scheduled Draft",
                releaseType = ReleaseType.Single,
                releaseDate = "2026-06-30T00:00:00Z",
                releaseGroupId = (Guid?)null,
            },
            JsonOptions);

        var release = await createRelease.Content.ReadFromJsonAsync<ManageReleaseDetailResponse>(JsonOptions);
        Assert.NotNull(release);

        var createTrack = await client.PostAsJsonAsync(
            $"/api/v1/catalog/releases/{release.Id}/tracks",
            new { title = "Only Track", trackNumber = 1, durationMs = 180_000, explicitFlag = false },
            JsonOptions);
        var track = await createTrack.Content.ReadFromJsonAsync<ManageTrackResponse>(JsonOptions);
        Assert.NotNull(track);

        await using (var scope = fixture.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
            var entity = await db.Tracks.FirstAsync(t => t.Id == TrackId.From(track.Id));
            entity.SetAudioStream($"dash/{track.Id}/{Guid.CreateVersion7()}/manifest.mpd");
            entity.MarkReady();
            await db.SaveChangesAsync();
        }

        var schedule = await client.PostAsync($"/api/v1/catalog/releases/{release.Id}/schedule", null);
        schedule.EnsureSuccessStatusCode();

        var cancel = await client.PostAsync($"/api/v1/catalog/releases/{release.Id}/cancel-schedule", null);
        Assert.Equal(HttpStatusCode.OK, cancel.StatusCode);
        var cancelled = await cancel.Content.ReadFromJsonAsync<ManageReleaseDetailResponse>(JsonOptions);
        Assert.NotNull(cancelled);
        Assert.Equal(ReleaseLifecycleStatus.Draft, cancelled.LifecycleStatus);
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
}

