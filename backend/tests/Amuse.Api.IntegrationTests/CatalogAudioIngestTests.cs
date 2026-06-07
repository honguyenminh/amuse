using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Amuse.Domain.Catalog;
using Amuse.Domain.Tenancy;
using Amuse.Modules.Catalog.Features.ManageTrackAudio;
using Amuse.Modules.Catalog.Features.Shared;
using Amuse.Modules.Catalog.Messaging;
using Amuse.Modules.Catalog.Persistence;
using Amuse.Modules.Catalog.Processing;
using Amuse.Modules.Identity.Features.Shared;
using Amuse.Modules.Media;
using Amuse.Modules.Tenancy.Features.Shared;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Amuse.Api.IntegrationTests;

[Collection(AmuseApiCollection.Name)]
public sealed class CatalogAudioIngestTests(AmuseApiFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    [Fact]
    public async Task Complete_without_intent_fails()
    {
        using var client = await CreateOrgClientAsync($"ingest-no-intent-{Guid.CreateVersion7():N}@amuse.test");
        var trackId = await CreateDraftTrackAsync(client);

        var masterKey = $"masters/{trackId}/{Guid.CreateVersion7()}.wav";
        await fixture.ObjectStorage.PutAsync(
            MediaBucket.Audio,
            masterKey,
            new byte[] { 0x00, 0x01 },
            "audio/wav",
            CancellationToken.None);

        var complete = await client.PostAsJsonAsync(
            $"/api/v1/catalog/tracks/{trackId}/audio-master/complete",
            new { key = masterKey },
            JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, complete.StatusCode);
        var problem = await complete.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal("catalog.invalid_audio_upload_request", problem.GetProperty("title").GetString());
    }

    [Fact]
    public async Task Complete_with_wrong_track_prefix_fails()
    {
        using var client = await CreateOrgClientAsync($"ingest-wrong-prefix-{Guid.CreateVersion7():N}@amuse.test");
        var trackId = await CreateDraftTrackAsync(client);
        var otherTrackId = Guid.CreateVersion7();

        var presign = await client.PostAsJsonAsync(
            $"/api/v1/catalog/tracks/{trackId}/audio-master/presign-upload",
            new { fileName = "track.wav", contentType = "audio/wav" },
            JsonOptions);
        presign.EnsureSuccessStatusCode();
        var presigned = await presign.Content.ReadFromJsonAsync<PresignAudioMasterUploadResponse>(JsonOptions);
        Assert.NotNull(presigned);

        await fixture.ObjectStorage.PutAsync(
            MediaBucket.Audio,
            presigned.Key,
            new byte[] { 0x00, 0x01 },
            "audio/wav",
            CancellationToken.None);

        var wrongKey = presigned.Key.Replace(trackId.ToString(), otherTrackId.ToString(), StringComparison.Ordinal);
        var complete = await client.PostAsJsonAsync(
            $"/api/v1/catalog/tracks/{trackId}/audio-master/complete",
            new { key = wrongKey },
            JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, complete.StatusCode);
    }

    [Fact]
    public async Task Complete_happy_path_enqueues_outbox_and_dispatches()
    {
        using var client = await CreateOrgClientAsync($"ingest-happy-{Guid.CreateVersion7():N}@amuse.test");
        var trackId = await CreateDraftTrackAsync(client);
        var queue = fixture.Services.GetRequiredService<IAudioTranscodeJobQueue>() as InMemoryAudioTranscodeJobQueue;
        Assert.NotNull(queue);
        await CatalogOutboxTestSupport.DrainPendingAsync(fixture.Services);
        var beforeCount = queue.Messages.Count;

        var presign = await client.PostAsJsonAsync(
            $"/api/v1/catalog/tracks/{trackId}/audio-master/presign-upload",
            new { fileName = "track.wav", contentType = "audio/wav" },
            JsonOptions);
        presign.EnsureSuccessStatusCode();
        var presigned = await presign.Content.ReadFromJsonAsync<PresignAudioMasterUploadResponse>(JsonOptions);
        Assert.NotNull(presigned);

        await fixture.ObjectStorage.PutAsync(
            MediaBucket.Audio,
            presigned.Key,
            new byte[] { 0x00, 0x01 },
            "audio/wav",
            CancellationToken.None);

        var complete = await client.PostAsJsonAsync(
            $"/api/v1/catalog/tracks/{trackId}/audio-master/complete",
            new { key = presigned.Key },
            JsonOptions);
        Assert.Equal(HttpStatusCode.OK, complete.StatusCode);

        await using (var scope = fixture.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
            var outboxCount = await db.CatalogOutboxMessages.CountAsync(
                m => m.ProcessedAt == null && m.MessageType == CatalogOutboxMessage.AudioTranscodeJobType);
            Assert.True(outboxCount >= 1);
        }

        await CatalogOutboxTestSupport.DrainPendingAsync(fixture.Services);
        Assert.Equal(beforeCount + 1, queue.Messages.Count);
    }

    [Fact]
    public async Task Retry_republishes_when_outbox_processed_but_queue_empty()
    {
        using var client = await CreateOrgClientAsync($"ingest-retry-{Guid.CreateVersion7():N}@amuse.test");
        var trackId = await CreateTrackWithFailedTranscodeAsync(client);
        var queue = fixture.Services.GetRequiredService<IAudioTranscodeJobQueue>() as InMemoryAudioTranscodeJobQueue;
        Assert.NotNull(queue);
        queue.Messages.Clear();

        await using (var scope = fixture.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
            var job = await db.AudioTranscodeJobs
                .Where(j => j.TrackId == TrackId.From(trackId))
                .OrderByDescending(j => j.CreatedAt)
                .FirstAsync();
            job.MarkFailed("forced", DateTimeOffset.UtcNow);
            await db.SaveChangesAsync();
        }

        var retry = await client.PostAsync(
            $"/api/v1/catalog/tracks/{trackId}/ingestion/retry-transcode",
            null);
        retry.EnsureSuccessStatusCode();

        await CatalogOutboxTestSupport.DrainPendingAsync(fixture.Services);
        Assert.NotEmpty(queue.Messages);
    }

    [Fact]
    public async Task Reupload_clears_stream_key()
    {
        using var client = await CreateOrgClientAsync($"ingest-reupload-{Guid.CreateVersion7():N}@amuse.test");
        var trackId = await CreateDraftTrackAsync(client);

        await using (var scope = fixture.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
            var track = await db.Tracks.FirstAsync(t => t.Id == TrackId.From(trackId));
            track.SetAudioStream($"dash/{trackId}/{Guid.CreateVersion7()}/manifest.mpd");
            track.MarkReady();
            await db.SaveChangesAsync();
        }

        var presign = await client.PostAsJsonAsync(
            $"/api/v1/catalog/tracks/{trackId}/audio-master/presign-upload",
            new { fileName = "track.wav", contentType = "audio/wav" },
            JsonOptions);
        presign.EnsureSuccessStatusCode();
        var presigned = await presign.Content.ReadFromJsonAsync<PresignAudioMasterUploadResponse>(JsonOptions);
        Assert.NotNull(presigned);

        await fixture.ObjectStorage.PutAsync(
            MediaBucket.Audio,
            presigned.Key,
            new byte[] { 0x00, 0x01 },
            "audio/wav",
            CancellationToken.None);

        var complete = await client.PostAsJsonAsync(
            $"/api/v1/catalog/tracks/{trackId}/audio-master/complete",
            new { key = presigned.Key },
            JsonOptions);
        complete.EnsureSuccessStatusCode();

        await using (var scope = fixture.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
            var track = await db.Tracks.AsNoTracking().FirstAsync(t => t.Id == TrackId.From(trackId));
            Assert.Null(track.AudioStreamKey);
            Assert.Equal(TrackLifecycleStatus.Processing, track.LifecycleStatus);
        }
    }

    private async Task<HttpClient> CreateOrgClientAsync(string email)
    {
        var client = fixture.CreateClient();
        const string password = "Password1234!";
        await RegisterAndConfirmAsync(client, email, password);
        var accountTokens = await LoginAccountTokensAsync(client, email, password);
        var org = await CreateOrganizationAsync(client, accountTokens.AccessToken, "Ingest Test Org");
        var orgTokens = await RefreshOrgPersonaAsync(client, accountTokens.RefreshToken!, org.Id);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", orgTokens.AccessToken);
        return client;
    }

    private async Task<Guid> CreateDraftTrackAsync(HttpClient client)
    {
        var artists = await client.GetFromJsonAsync<ManageArtistListResponse>(
            "/api/v1/catalog/manage/artists",
            JsonOptions);
        Assert.NotNull(artists);
        var artistId = artists.Items[0].Id;

        var createRelease = await client.PostAsJsonAsync(
            $"/api/v1/catalog/artists/{artistId}/releases",
            new
            {
                title = $"Ingest {Guid.CreateVersion7():N}",
                releaseType = ReleaseType.Single,
                releaseDate = "2026-06-01T00:00:00Z",
            },
            JsonOptions);
        createRelease.EnsureSuccessStatusCode();
        var release = await createRelease.Content.ReadFromJsonAsync<ManageReleaseDetailResponse>(JsonOptions);
        Assert.NotNull(release);

        var createTrack = await client.PostAsJsonAsync(
            $"/api/v1/catalog/releases/{release.Id}/tracks",
            new
            {
                title = "Ingest Track",
                trackNumber = 1,
                durationMs = 1,
                explicitFlag = false,
            },
            JsonOptions);
        createTrack.EnsureSuccessStatusCode();
        var track = await createTrack.Content.ReadFromJsonAsync<ManageTrackResponse>(JsonOptions);
        Assert.NotNull(track);
        return track.Id;
    }

    private async Task<Guid> CreateTrackWithFailedTranscodeAsync(HttpClient client)
    {
        var trackId = await CreateDraftTrackAsync(client);

        var presign = await client.PostAsJsonAsync(
            $"/api/v1/catalog/tracks/{trackId}/audio-master/presign-upload",
            new { fileName = "track.wav", contentType = "audio/wav" },
            JsonOptions);
        presign.EnsureSuccessStatusCode();
        var presigned = await presign.Content.ReadFromJsonAsync<PresignAudioMasterUploadResponse>(JsonOptions);
        Assert.NotNull(presigned);

        await fixture.ObjectStorage.PutAsync(
            MediaBucket.Audio,
            presigned.Key,
            new byte[] { 0x00, 0x01 },
            "audio/wav",
            CancellationToken.None);

        var complete = await client.PostAsJsonAsync(
            $"/api/v1/catalog/tracks/{trackId}/audio-master/complete",
            new { key = presigned.Key },
            JsonOptions);
        complete.EnsureSuccessStatusCode();
        await CatalogOutboxTestSupport.DrainPendingAsync(fixture.Services);

        return trackId;
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

    private static async Task<OrganizationResponse> CreateOrganizationAsync(
        HttpClient client,
        string accessToken,
        string displayName)
    {
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);

        var createOrg = await client.PostAsJsonAsync(
            "/api/v1/tenancy/organizations",
            new
            {
                displayName,
                orgClass = OrganizationClass.IndieGroup,
                createDefaultArtist = true,
            },
            JsonOptions);
        createOrg.EnsureSuccessStatusCode();
        return (await createOrg.Content.ReadFromJsonAsync<OrganizationResponse>(JsonOptions))!;
    }
}
