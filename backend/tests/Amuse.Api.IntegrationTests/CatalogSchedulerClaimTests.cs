using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Amuse.Domain.Catalog;
using Amuse.Domain.Tenancy;
using Amuse.Modules.Catalog.Features.Common;
using Amuse.Modules.Catalog.Features.Common;
using Amuse.Modules.Catalog.Persistence;
using Amuse.Modules.Catalog.Services;
using Amuse.Modules.Identity.Features.Common;
using Amuse.Modules.Media;
using Amuse.Modules.Tenancy.Features.Common;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Amuse.Api.IntegrationTests;

[Collection(AmuseApiCollection.Name)]
public sealed class CatalogSchedulerClaimTests(AmuseApiFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    [Fact]
    public async Task ClaimDueReleaseIds_skips_rows_locked_by_another_transaction()
    {
        using var client = fixture.CreateClient();
        var releaseId = await CreateScheduledReleaseDueNowAsync(client);

        await using var scope1 = fixture.Services.CreateAsyncScope();
        await using var scope2 = fixture.Services.CreateAsyncScope();

        var db1 = scope1.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var db2 = scope2.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var claim1 = scope1.ServiceProvider.GetRequiredService<ScheduledReleaseClaimService>();
        var claim2 = scope2.ServiceProvider.GetRequiredService<ScheduledReleaseClaimService>();

        var now = DateTimeOffset.UtcNow.AddMinutes(1);
        IReadOnlyList<ReleaseId> firstClaim = [];
        IReadOnlyList<ReleaseId> secondClaimWhileLocked = [];

        var strategy1 = db1.Database.CreateExecutionStrategy();
        await strategy1.ExecuteAsync(async () =>
        {
            await using var tx1 = await db1.Database.BeginTransactionAsync();
            firstClaim = await claim1.ClaimDueReleaseIdsAsync(now, 50, CancellationToken.None);
            Assert.Contains(firstClaim, id => id.Value == releaseId);

            var strategy2 = db2.Database.CreateExecutionStrategy();
            await strategy2.ExecuteAsync(async () =>
            {
                await using var tx2 = await db2.Database.BeginTransactionAsync();
                secondClaimWhileLocked = await claim2.ClaimDueReleaseIdsAsync(now, 50, CancellationToken.None);
                Assert.DoesNotContain(secondClaimWhileLocked, id => id.Value == releaseId);
                await tx2.CommitAsync();
            });

            await tx1.CommitAsync();
        });

        IReadOnlyList<ReleaseId> claimAfterRelease = [];
        var strategy3 = db1.Database.CreateExecutionStrategy();
        await strategy3.ExecuteAsync(async () =>
        {
            await using var tx3 = await db1.Database.BeginTransactionAsync();
            claimAfterRelease = await claim1.ClaimDueReleaseIdsAsync(now, 50, CancellationToken.None);
            await tx3.CommitAsync();
        });

        Assert.Contains(claimAfterRelease, id => id.Value == releaseId);
    }

    private async Task<Guid> CreateScheduledReleaseDueNowAsync(HttpClient client)
    {
        const string email = "catalog-skip-locked@amuse.test";
        const string password = "Password1234!";

        await RegisterAndConfirmAsync(client, email, password);
        var accountTokens = await LoginAccountTokensAsync(client, email, password);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accountTokens.AccessToken);

        var createOrg = await client.PostAsJsonAsync(
            "/api/v1/tenancy/organizations",
            new
            {
                displayName = "Skip Locked Indie",
                orgClass = OrganizationClass.IndieGroup,
                createDefaultArtist = true,
            },
            JsonOptions);
        createOrg.EnsureSuccessStatusCode();
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
                title = "Due Now Single",
                releaseType = ReleaseType.Single,
                releaseDate = DateTimeOffset.UtcNow.AddHours(1).ToString("O"),
            },
            JsonOptions);
        createRelease.EnsureSuccessStatusCode();
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
            new byte[] { 0x01 },
            "audio/wav",
            CancellationToken.None);

        var complete = await client.PostAsJsonAsync(
            $"/api/v1/catalog/tracks/{track.Id}/audio-master/complete",
            new { key = presigned.Key },
            JsonOptions);
        complete.EnsureSuccessStatusCode();
        await CatalogOutboxTestSupport.DrainPendingAsync(fixture.Services);

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

        var releaseId = release.Id;

        await using (var scope = fixture.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
            await db.Database.ExecuteSqlAsync($"""
                UPDATE catalog.release
                SET release_date = {DateTimeOffset.UtcNow.AddMinutes(-5)}
                WHERE id = {releaseId}
                """);
        }

        return releaseId;
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
