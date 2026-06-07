using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Amuse.Domain.Catalog;
using Amuse.Domain.Tenancy;
using Amuse.Modules.Catalog.Features.Common;
using Amuse.Modules.Catalog.Features.Common;
using Amuse.Modules.Catalog.Persistence;
using Amuse.Modules.Catalog.Processing;
using Amuse.Modules.Identity.Features.Common;
using Amuse.Modules.Media;
using Amuse.Modules.Tenancy.Features.Common;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Amuse.Api.IntegrationTests;

[Collection(AmuseApiCollection.Name)]
public sealed class CatalogDeleteTests(AmuseApiFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    [Fact]
    public async Task Delete_track_removes_row_and_media_objects()
    {
        using var client = fixture.CreateClient();
        var context = await CreateOrgCatalogContextAsync(client, "catalog-delete-track@amuse.test");

        var createRelease = await client.PostAsJsonAsync(
            $"/api/v1/catalog/artists/{context.ArtistId}/releases",
            new
            {
                title = "Delete Track Single",
                releaseType = ReleaseType.Single,
                releaseDate = "2026-06-01T00:00:00Z",
            },
            JsonOptions);
        Assert.Equal(HttpStatusCode.Created, createRelease.StatusCode);
        var release = await createRelease.Content.ReadFromJsonAsync<ManageReleaseDetailResponse>(JsonOptions);
        Assert.NotNull(release);

        var createTrack = await client.PostAsJsonAsync(
            $"/api/v1/catalog/releases/{release.Id}/tracks",
            new
            {
                title = "Disposable Track",
                trackNumber = 1,
                durationMs = 180_000,
                explicitFlag = false,
            },
            JsonOptions);
        Assert.Equal(HttpStatusCode.Created, createTrack.StatusCode);
        var track = await createTrack.Content.ReadFromJsonAsync<ManageTrackResponse>(JsonOptions);
        Assert.NotNull(track);

        var masterKey = $"masters/{track.Id}/{Guid.CreateVersion7()}.wav";
        var dashPrefix = $"dash/{track.Id}/{Guid.CreateVersion7()}/";
        var streamKey = $"{dashPrefix}manifest.mpd";
        var dashSegmentKey = $"{dashPrefix}chunk.m4s";

        await fixture.ObjectStorage.PutAsync(
            MediaBucket.Audio,
            masterKey,
            new byte[] { 0x00, 0x01 },
            "audio/wav",
            CancellationToken.None);
        await fixture.ObjectStorage.PutAsync(
            MediaBucket.Audio,
            dashSegmentKey,
            new byte[] { 0x02, 0x03 },
            "video/mp4",
            CancellationToken.None);

        await using (var scope = fixture.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
            var entity = await db.Tracks.FirstAsync(t => t.Id == TrackId.From(track.Id));
            entity.SetAudioMaster(masterKey);
            entity.SetAudioStream(streamKey);
            db.AudioTranscodeJobs.Add(
                AudioTranscodeJob.Enqueue(entity.Id, masterKey, streamKey, DateTimeOffset.UtcNow));
            await db.SaveChangesAsync();
        }

        Assert.True(fixture.ObjectStorage.Contains(MediaBucket.Audio, masterKey));
        Assert.True(fixture.ObjectStorage.Contains(MediaBucket.Audio, dashSegmentKey));

        var delete = await client.DeleteAsync($"/api/v1/catalog/tracks/{track.Id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        await using (var scope = fixture.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
            Assert.False(await db.Tracks.AnyAsync(t => t.Id == TrackId.From(track.Id)));
            Assert.False(await db.AudioTranscodeJobs.AnyAsync(j => j.TrackId == TrackId.From(track.Id)));
        }

        Assert.False(fixture.ObjectStorage.Contains(MediaBucket.Audio, masterKey));
        Assert.False(fixture.ObjectStorage.Contains(MediaBucket.Audio, dashSegmentKey));

        var getRelease = await client.GetAsync($"/api/v1/catalog/manage/releases/{release.Id}");
        Assert.Equal(HttpStatusCode.OK, getRelease.StatusCode);
        var refreshed = await getRelease.Content.ReadFromJsonAsync<ManageReleaseDetailResponse>(JsonOptions);
        Assert.NotNull(refreshed);
        Assert.Empty(refreshed.Tracks);
    }

    [Fact]
    public async Task Delete_release_removes_tracks_cover_and_media_objects()
    {
        using var client = fixture.CreateClient();
        var context = await CreateOrgCatalogContextAsync(client, "catalog-delete-release@amuse.test");

        var createRelease = await client.PostAsJsonAsync(
            $"/api/v1/catalog/artists/{context.ArtistId}/releases",
            new
            {
                title = "Delete Release EP",
                releaseType = ReleaseType.Ep,
                releaseDate = "2026-06-01T00:00:00Z",
            },
            JsonOptions);
        Assert.Equal(HttpStatusCode.Created, createRelease.StatusCode);
        var release = await createRelease.Content.ReadFromJsonAsync<ManageReleaseDetailResponse>(JsonOptions);
        Assert.NotNull(release);

        var createTrack = await client.PostAsJsonAsync(
            $"/api/v1/catalog/releases/{release.Id}/tracks",
            new
            {
                title = "Track A",
                trackNumber = 1,
                durationMs = 120_000,
                explicitFlag = false,
            },
            JsonOptions);
        Assert.Equal(HttpStatusCode.Created, createTrack.StatusCode);
        var track = await createTrack.Content.ReadFromJsonAsync<ManageTrackResponse>(JsonOptions);
        Assert.NotNull(track);

        var masterKey = $"masters/{track.Id}/master.wav";
        var dashPrefix = $"dash/{track.Id}/{Guid.CreateVersion7()}/";
        var streamKey = $"{dashPrefix}manifest.mpd";
        var coverKey = $"covers/{release.Id}/cover.jpg";
        await fixture.ObjectStorage.PutAsync(
            MediaBucket.Audio,
            masterKey,
            new byte[] { 0x10 },
            "audio/wav",
            CancellationToken.None);
        await fixture.ObjectStorage.PutAsync(
            MediaBucket.Covers,
            coverKey,
            new byte[] { 0xFF, 0xD8 },
            "image/jpeg",
            CancellationToken.None);

        await using (var scope = fixture.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
            var releaseEntity = await db.Releases
                .Include(r => r.Tracks)
                .FirstAsync(r => r.Id == ReleaseId.From(release.Id));
            var trackEntity = releaseEntity.Tracks.Single();
            trackEntity.SetAudioMaster(masterKey);
            trackEntity.SetAudioStream(streamKey);
            releaseEntity.SetCoverArtKey(coverKey, DateTimeOffset.UtcNow);
            await db.SaveChangesAsync();
        }

        var delete = await client.DeleteAsync($"/api/v1/catalog/releases/{release.Id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        await using (var scope = fixture.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
            Assert.False(await db.Releases.AnyAsync(r => r.Id == ReleaseId.From(release.Id)));
            Assert.False(await db.Tracks.AnyAsync(t => t.ReleaseId == ReleaseId.From(release.Id)));
        }

        Assert.False(fixture.ObjectStorage.Contains(MediaBucket.Audio, masterKey));
        Assert.False(fixture.ObjectStorage.Contains(MediaBucket.Covers, coverKey));

        var getRelease = await client.GetAsync($"/api/v1/catalog/manage/releases/{release.Id}");
        Assert.Equal(HttpStatusCode.BadRequest, getRelease.StatusCode);
    }

    [Fact]
    public async Task Delete_published_release_is_rejected()
    {
        using var client = fixture.CreateClient();
        var context = await CreateOrgCatalogContextAsync(client, "catalog-delete-published@amuse.test");

        var createRelease = await client.PostAsJsonAsync(
            $"/api/v1/catalog/artists/{context.ArtistId}/releases",
            new
            {
                title = "Published Only",
                releaseType = ReleaseType.Single,
                releaseDate = "2026-06-01T00:00:00Z",
            },
            JsonOptions);
        var release = await createRelease.Content.ReadFromJsonAsync<ManageReleaseDetailResponse>(JsonOptions);
        Assert.NotNull(release);

        var createTrack = await client.PostAsJsonAsync(
            $"/api/v1/catalog/releases/{release.Id}/tracks",
            new
            {
                title = "Track",
                trackNumber = 1,
                durationMs = 120_000,
                explicitFlag = false,
            },
            JsonOptions);
        var track = await createTrack.Content.ReadFromJsonAsync<ManageTrackResponse>(JsonOptions);
        Assert.NotNull(track);

        await using (var scope = fixture.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
            var entity = await db.Tracks.FirstAsync(t => t.Id == TrackId.From(track.Id));
            entity.SetAudioStream($"dash/{track.Id}/manifest.mpd");
            entity.MarkReady();
            await db.SaveChangesAsync();
        }

        var publish = await client.PostAsync($"/api/v1/catalog/releases/{release.Id}/publish", null);
        Assert.Equal(HttpStatusCode.OK, publish.StatusCode);

        var delete = await client.DeleteAsync($"/api/v1/catalog/releases/{release.Id}");
        Assert.Equal(HttpStatusCode.BadRequest, delete.StatusCode);
    }

    private async Task<CatalogTestContext> CreateOrgCatalogContextAsync(HttpClient client, string email)
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
                displayName = "Delete Test Indie",
                orgClass = OrganizationClass.IndieGroup,
                createDefaultArtist = true,
            },
            JsonOptions);
        createOrg.EnsureSuccessStatusCode();
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

        return new CatalogTestContext(artists.Items[0].Id);
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

    private sealed record CatalogTestContext(Guid ArtistId);
}
