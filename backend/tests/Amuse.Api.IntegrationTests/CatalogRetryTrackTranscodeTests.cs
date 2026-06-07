using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Amuse.Domain.Catalog;
using Amuse.Domain.Tenancy;
using Amuse.Modules.Catalog.Features.RetryTrackTranscode;
using Amuse.Modules.Catalog.Features.Shared;
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
public sealed class CatalogRetryTrackTranscodeTests(AmuseApiFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    [Fact]
    public async Task Retry_failed_job_creates_new_queued_job_and_publishes_message()
    {
        using var client = fixture.CreateClient();
        var trackId = await ProvisionFailedTrackAsync(client, "retry-success");
        var queue = fixture.Services.GetRequiredService<IAudioTranscodeJobQueue>() as InMemoryAudioTranscodeJobQueue;
        Assert.NotNull(queue);
        await CatalogOutboxTestSupport.DrainPendingAsync(fixture.Services);
        var beforeCount = queue.Messages.Count;

        var retry = await client.PostAsync(
            $"/api/v1/catalog/tracks/{trackId}/ingestion/retry-transcode",
            null);

        Assert.Equal(HttpStatusCode.OK, retry.StatusCode);
        var response = await retry.Content.ReadFromJsonAsync<RetryTrackTranscodeResponse>(JsonOptions);
        Assert.NotNull(response);
        Assert.Equal(trackId, response.TrackId);
        Assert.Equal(AudioTranscodeJobStatus.Queued, response.JobStatus);
        Assert.False(response.ReusedInflightJob);
        await CatalogOutboxTestSupport.DrainPendingAsync(fixture.Services);
        Assert.Equal(beforeCount + 1, queue.Messages.Count);

        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var latest = await db.AudioTranscodeJobs
            .Where(j => j.TrackId == TrackId.From(trackId))
            .OrderByDescending(j => j.CreatedAt)
            .FirstAsync();

        Assert.Equal(response.JobId, latest.Id);
        Assert.Equal(AudioTranscodeJobStatus.Queued, latest.Status);
    }

    [Fact]
    public async Task Retry_with_existing_inflight_job_returns_same_job_without_republishing()
    {
        using var client = fixture.CreateClient();
        var trackId = await ProvisionFailedTrackAsync(client, "retry-inflight");
        var queue = fixture.Services.GetRequiredService<IAudioTranscodeJobQueue>() as InMemoryAudioTranscodeJobQueue;
        Assert.NotNull(queue);

        var first = await client.PostAsync(
            $"/api/v1/catalog/tracks/{trackId}/ingestion/retry-transcode",
            null);
        first.EnsureSuccessStatusCode();
        var firstResponse = await first.Content.ReadFromJsonAsync<RetryTrackTranscodeResponse>(JsonOptions);
        Assert.NotNull(firstResponse);
        await CatalogOutboxTestSupport.DrainPendingAsync(fixture.Services);
        var afterFirstPublishCount = queue.Messages.Count;

        var second = await client.PostAsync(
            $"/api/v1/catalog/tracks/{trackId}/ingestion/retry-transcode",
            null);

        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        var secondResponse = await second.Content.ReadFromJsonAsync<RetryTrackTranscodeResponse>(JsonOptions);
        Assert.NotNull(secondResponse);
        Assert.True(secondResponse.ReusedInflightJob);
        Assert.Equal(firstResponse.JobId, secondResponse.JobId);
        Assert.Equal(afterFirstPublishCount, queue.Messages.Count);
    }

    [Fact]
    public async Task Retry_track_from_different_org_returns_scope_problem()
    {
        using var client = fixture.CreateClient();
        var userAEmail = $"retry-scope-a-{Guid.CreateVersion7():N}@amuse.test";
        var userBEmail = $"retry-scope-b-{Guid.CreateVersion7():N}@amuse.test";
        const string password = "Password1234!";

        await RegisterAndConfirmAsync(client, userAEmail, password);
        var userATokens = await LoginAccountTokensAsync(client, userAEmail, password);
        var orgA = await CreateOrganizationAsync(client, userATokens.AccessToken, "Retry Org A");
        var orgATokens = await RefreshOrgPersonaAsync(client, userATokens.RefreshToken!, orgA.Id);
        var failedTrackId = await CreateTrackWithFailedTranscodeAsync(client, orgATokens.AccessToken);

        await RegisterAndConfirmAsync(client, userBEmail, password);
        var userBTokens = await LoginAccountTokensAsync(client, userBEmail, password);
        var orgB = await CreateOrganizationAsync(client, userBTokens.AccessToken, "Retry Org B");
        var orgBTokens = await RefreshOrgPersonaAsync(client, userBTokens.RefreshToken!, orgB.Id);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", orgBTokens.AccessToken);

        var retry = await client.PostAsync(
            $"/api/v1/catalog/tracks/{failedTrackId}/ingestion/retry-transcode",
            null);

        Assert.Equal(HttpStatusCode.BadRequest, retry.StatusCode);
        var problem = await retry.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal("catalog.not_organization_catalog", problem.GetProperty("title").GetString());
    }

    private async Task<Guid> ProvisionFailedTrackAsync(HttpClient client, string prefix)
    {
        var email = $"{prefix}-{Guid.CreateVersion7():N}@amuse.test";
        const string password = "Password1234!";

        await RegisterAndConfirmAsync(client, email, password);
        var accountTokens = await LoginAccountTokensAsync(client, email, password);
        var org = await CreateOrganizationAsync(client, accountTokens.AccessToken, $"{prefix} org");
        var orgTokens = await RefreshOrgPersonaAsync(client, accountTokens.RefreshToken!, org.Id);

        return await CreateTrackWithFailedTranscodeAsync(client, orgTokens.AccessToken);
    }

    private async Task<Guid> CreateTrackWithFailedTranscodeAsync(HttpClient client, string orgAccessToken)
    {
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", orgAccessToken);

        var artists = await client.GetFromJsonAsync<ManageArtistListResponse>(
            "/api/v1/catalog/manage/artists",
            JsonOptions);
        Assert.NotNull(artists);
        Assert.NotEmpty(artists.Items);
        var artistId = artists.Items[0].Id;

        var createRelease = await client.PostAsJsonAsync(
            $"/api/v1/catalog/artists/{artistId}/releases",
            new
            {
                title = $"Retry {Guid.CreateVersion7():N}",
                releaseType = ReleaseType.Single,
                releaseDate = "2026-06-01T00:00:00Z",
                releaseGroupId = (Guid?)null,
            },
            JsonOptions);
        createRelease.EnsureSuccessStatusCode();
        var release = await createRelease.Content.ReadFromJsonAsync<ManageReleaseDetailResponse>(JsonOptions);
        Assert.NotNull(release);

        var createTrack = await client.PostAsJsonAsync(
            $"/api/v1/catalog/releases/{release.Id}/tracks",
            new
            {
                title = "Retry Track",
                trackNumber = 1,
                durationMs = 180_000,
                explicitFlag = false,
            },
            JsonOptions);
        createTrack.EnsureSuccessStatusCode();
        var track = await createTrack.Content.ReadFromJsonAsync<ManageTrackResponse>(JsonOptions);
        Assert.NotNull(track);

        var presign = await client.PostAsJsonAsync(
            $"/api/v1/catalog/tracks/{track.Id}/audio-master/presign-upload",
            new { fileName = "track.wav", contentType = "audio/wav" },
            JsonOptions);
        presign.EnsureSuccessStatusCode();
        var presigned = await presign.Content.ReadFromJsonAsync<Amuse.Modules.Catalog.Features.ManageTrackAudio.PresignAudioMasterUploadResponse>(JsonOptions);
        Assert.NotNull(presigned);

        await fixture.ObjectStorage.PutAsync(
            MediaBucket.Audio,
            presigned.Key,
            new byte[] { 0x00, 0x01, 0x02 },
            "audio/wav",
            CancellationToken.None);

        var complete = await client.PostAsJsonAsync(
            $"/api/v1/catalog/tracks/{track.Id}/audio-master/complete",
            new { key = presigned.Key },
            JsonOptions);
        complete.EnsureSuccessStatusCode();
        await CatalogOutboxTestSupport.DrainPendingAsync(fixture.Services);

        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var typedTrackId = TrackId.From(track.Id);
        var failedJob = await db.AudioTranscodeJobs
            .Where(j => j.TrackId == typedTrackId)
            .OrderByDescending(j => j.CreatedAt)
            .FirstAsync();
        failedJob.MarkFailed("forced test failure", DateTimeOffset.UtcNow);
        await db.SaveChangesAsync();

        return track.Id;
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
