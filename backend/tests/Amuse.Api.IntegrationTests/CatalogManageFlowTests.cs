using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Amuse.Domain.Catalog;
using Amuse.Domain.Tenancy;
using Amuse.Modules.Catalog.Features.GetResourceAudit;
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
public sealed class CatalogManageFlowTests(AmuseApiFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    [Fact]
    public async Task Indie_org_can_create_release_publish_and_browse_publicly()
    {
        using var client = fixture.CreateClient();
        const string email = "catalog-flow@amuse.test";
        const string password = "Password1234!";

        await RegisterAndConfirmAsync(client, email, password);
        var accountTokens = await LoginAccountTokensAsync(client, email, password);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accountTokens.AccessToken);

        var createOrg = await client.PostAsJsonAsync(
            "/api/v1/tenancy/organizations",
            new
            {
                displayName = "Catalog Flow Indie",
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

        var createGroup = await client.PostAsJsonAsync(
            $"/api/v1/catalog/artists/{artistId}/release-groups",
            new
            {
                title = "Flow Era",
                description = "Main era for metadata flow.",
            },
            JsonOptions);
        Assert.Equal(HttpStatusCode.Created, createGroup.StatusCode);
        var group = await createGroup.Content.ReadFromJsonAsync<ManageReleaseGroupResponse>(JsonOptions);
        Assert.NotNull(group);

        var createRelease = await client.PostAsJsonAsync(
            $"/api/v1/catalog/artists/{artistId}/releases",
            new
            {
                title = "Flow Single",
                releaseType = ReleaseType.Single,
                releaseDate = "2026-05-31T00:00:00Z",
                releaseGroupId = group.Id,
                description = "Release metadata description",
                upc = "123456789012",
                primaryGenre = "Indie Pop",
                tags = "indie,pop",
                languageCode = "en",
                labelName = "Flow Label",
                pLine = "℗ 2026 Flow Label",
                cLine = "© 2026 Flow Label",
                metadataComplete = true,
            },
            JsonOptions);

        Assert.Equal(HttpStatusCode.Created, createRelease.StatusCode);
        var release = await createRelease.Content.ReadFromJsonAsync<ManageReleaseDetailResponse>(JsonOptions);
        Assert.NotNull(release);
        Assert.Equal(group.Id, release.ReleaseGroupId);
        Assert.Equal("123456789012", release.Upc);
        Assert.True(release.MetadataComplete);

        var createTrack = await client.PostAsJsonAsync(
            $"/api/v1/catalog/releases/{release.Id}/tracks",
            new
            {
                title = "Only Track",
                trackNumber = 1,
                durationMs = 180_000,
                explicitFlag = false,
                isrc = "US1A22600001",
                languageCode = "en",
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

        var presign = await client.PostAsJsonAsync(
            $"/api/v1/catalog/tracks/{track.Id}/audio-master/presign-upload",
            new { fileName = "track.wav", contentType = "audio/wav" },
            JsonOptions);
        Assert.Equal(HttpStatusCode.OK, presign.StatusCode);

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

        var publish = await client.PostAsync(
            $"/api/v1/catalog/releases/{release.Id}/publish",
            null);
        Assert.Equal(HttpStatusCode.OK, publish.StatusCode);

        client.DefaultRequestHeaders.Remove("Authorization");
        var publicRelease = await client.GetAsync($"/api/v1/catalog/releases/{release.Id}");
        Assert.Equal(HttpStatusCode.OK, publicRelease.StatusCode);
    }

    [Fact]
    public async Task Catalog_resource_updates_are_audited_and_listable()
    {
        using var client = fixture.CreateClient();
        const string email = "catalog-audit@amuse.test";
        const string password = "Password1234!";

        await RegisterAndConfirmAsync(client, email, password);
        var accountTokens = await LoginAccountTokensAsync(client, email, password);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accountTokens.AccessToken);

        var createOrg = await client.PostAsJsonAsync(
            "/api/v1/tenancy/organizations",
            new
            {
                displayName = "Catalog Audit Indie",
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

        var createArtist = await client.PostAsJsonAsync(
            "/api/v1/catalog/artists",
            new
            {
                name = "Audit Artist",
                slug = "audit-artist",
                bio = "Initial bio.",
            },
            JsonOptions);
        Assert.Equal(HttpStatusCode.Created, createArtist.StatusCode);
        var artist = await createArtist.Content.ReadFromJsonAsync<ManageArtistSummaryResponse>(JsonOptions);
        Assert.NotNull(artist);

        var updateArtist = await client.PatchAsJsonAsync(
            $"/api/v1/catalog/artists/{artist.Id}",
            new
            {
                name = artist.Name,
                bio = "Updated bio for audit trail.",
                countryCode = "US",
            },
            JsonOptions);
        Assert.Equal(HttpStatusCode.OK, updateArtist.StatusCode);

        var audit = await client.GetFromJsonAsync<CatalogAuditListResponse>(
            $"/api/v1/catalog/manage/audit?tableName=catalog.artist&targetId={artist.Id}",
            JsonOptions);
        Assert.NotNull(audit);
        Assert.Contains(audit.Items, entry => entry.Action == "created");
        Assert.Contains(audit.Items, entry => entry.Action == "updated");
        Assert.Contains(
            audit.Items,
            entry => entry.Action == "updated" && entry.AfterJson?.Contains("Updated bio for audit trail.") == true);
    }

    [Fact]
    public async Task Release_cover_upload_is_audited()
    {
        using var client = fixture.CreateClient();
        const string email = "catalog-cover-audit@amuse.test";
        const string password = "Password1234!";

        await RegisterAndConfirmAsync(client, email, password);
        var accountTokens = await LoginAccountTokensAsync(client, email, password);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accountTokens.AccessToken);

        var createOrg = await client.PostAsJsonAsync(
            "/api/v1/tenancy/organizations",
            new
            {
                displayName = "Cover Audit Indie",
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

        var createRelease = await client.PostAsJsonAsync(
            $"/api/v1/catalog/artists/{artistId}/releases",
            new
            {
                title = "Cover Audit Single",
                releaseType = ReleaseType.Single,
                releaseDate = "2026-06-01T00:00:00Z",
            },
            JsonOptions);
        Assert.Equal(HttpStatusCode.Created, createRelease.StatusCode);
        var release = await createRelease.Content.ReadFromJsonAsync<ManageReleaseDetailResponse>(JsonOptions);
        Assert.NotNull(release);

        var presign = await client.PostAsJsonAsync(
            $"/api/v1/catalog/releases/{release.Id}/cover/presign-upload",
            new { fileName = "cover.jpg", contentType = "image/jpeg" },
            JsonOptions);
        Assert.Equal(HttpStatusCode.OK, presign.StatusCode);
        var presignResponse = await presign.Content.ReadFromJsonAsync<PresignReleaseCoverUploadResponse>(JsonOptions);
        Assert.NotNull(presignResponse);

        await fixture.ObjectStorage.PutAsync(
            MediaBucket.Covers,
            presignResponse.Key,
            new byte[] { 0xFF, 0xD8, 0xFF },
            "image/jpeg",
            CancellationToken.None);

        var complete = await client.PostAsJsonAsync(
            $"/api/v1/catalog/releases/{release.Id}/cover/complete",
            new { key = presignResponse.Key },
            JsonOptions);
        Assert.Equal(HttpStatusCode.OK, complete.StatusCode);

        var audit = await client.GetFromJsonAsync<CatalogAuditListResponse>(
            $"/api/v1/catalog/manage/audit?tableName=catalog.release&targetId={release.Id}",
            JsonOptions);
        Assert.NotNull(audit);
        Assert.Contains(
            audit.Items,
            entry => entry.Action == "updated"
                     && entry.AfterJson?.Contains(presignResponse.Key) == true);
    }

    [Fact]
    public async Task Release_without_group_id_auto_creates_artist_scoped_release_group()
    {
        using var client = fixture.CreateClient();
        const string email = "catalog-auto-group@amuse.test";
        const string password = "Password1234!";

        await RegisterAndConfirmAsync(client, email, password);
        var accountTokens = await LoginAccountTokensAsync(client, email, password);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accountTokens.AccessToken);

        var createOrg = await client.PostAsJsonAsync(
            "/api/v1/tenancy/organizations",
            new
            {
                displayName = "Auto Group Indie",
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

        var createRelease = await client.PostAsJsonAsync(
            $"/api/v1/catalog/artists/{artistId}/releases",
            new
            {
                title = "Auto Group Album",
                releaseType = ReleaseType.Album,
                releaseDate = "2026-05-31T00:00:00Z",
            },
            JsonOptions);

        Assert.Equal(HttpStatusCode.Created, createRelease.StatusCode);
        var release = await createRelease.Content.ReadFromJsonAsync<ManageReleaseDetailResponse>(JsonOptions);
        Assert.NotNull(release);
        Assert.NotNull(release.ReleaseGroupId);
        Assert.Equal("Auto Group Album", release.ReleaseGroupTitle);

        var groups = await client.GetFromJsonAsync<ManageReleaseGroupListResponse>(
            $"/api/v1/catalog/artists/{artistId}/release-groups",
            JsonOptions);
        Assert.NotNull(groups);
        Assert.Contains(groups.Items, g => g.Id == release.ReleaseGroupId && g.ArtistId == artistId);
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
