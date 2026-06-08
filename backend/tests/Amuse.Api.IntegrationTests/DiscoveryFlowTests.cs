using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Amuse.Domain.Catalog;
using Amuse.Domain.Discovery;
using Amuse.Modules.Catalog.Persistence;
using Amuse.Modules.Discovery.Persistence;
using Amuse.Modules.Identity.Features.Common;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Amuse.Api.IntegrationTests;

[Collection(AmuseApiCollection.Name)]
public sealed class DiscoveryFlowTests(AmuseApiFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task Discovery_flow_create_add_like_save_fork_and_private_transition()
    {
        fixture.CaptureEmailSender.Reset();
        using var client = fixture.CreateClient();
        var email = $"discovery-{Guid.CreateVersion7():N}@amuse.test";
        const string password = "Password1234!";

        await RegisterConfirmAndLoginListenerAsync(fixture, client, email, password);
        await CompleteOnboardingAsync(client);

        var (trackId, releaseId) = await GetSeededTrackAndReleaseAsync();

        var create = await client.PostAsJsonAsync(
            "/api/v1/discovery/playlists",
            new { title = "Integration Playlist", visibility = "public", description = "Test playlist" },
            JsonOptions);
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var playlist = await create.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var playlistId = playlist.GetProperty("id").GetGuid();

        var addTrack = await client.PostAsJsonAsync(
            $"/api/v1/discovery/playlists/{playlistId}/items",
            new { trackId },
            JsonOptions);
        Assert.Equal(HttpStatusCode.OK, addTrack.StatusCode);
        var addedItem = await addTrack.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal(trackId, addedItem.GetProperty("item").GetProperty("trackId").GetGuid());

        var like = await client.PutAsync($"/api/v1/discovery/liked/{trackId}", null);
        Assert.Equal(HttpStatusCode.NoContent, like.StatusCode);

        var saveRelease = await client.PutAsync($"/api/v1/discovery/library/releases/{releaseId}", null);
        Assert.Equal(HttpStatusCode.NoContent, saveRelease.StatusCode);

        var fork = await client.PostAsJsonAsync(
            $"/api/v1/discovery/playlists/{playlistId}/fork",
            new { title = "Integration Playlist (fork)" },
            JsonOptions);
        Assert.Equal(HttpStatusCode.Created, fork.StatusCode);
        var forked = await fork.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var forkId = forked.GetProperty("id").GetGuid();
        Assert.Equal(playlistId, forked.GetProperty("forkedFromPlaylistId").GetGuid());
        Assert.Equal("Integration Playlist (fork)", forked.GetProperty("title").GetString());

        var makePrivate = await client.PatchAsJsonAsync(
            $"/api/v1/discovery/playlists/{playlistId}",
            new { visibility = "private" },
            JsonOptions);
        Assert.Equal(HttpStatusCode.OK, makePrivate.StatusCode);

        await using var scope = fixture.Services.CreateAsyncScope();
        var discoveryDb = scope.ServiceProvider.GetRequiredService<DiscoveryDbContext>();
        var forkEntity = await discoveryDb.Playlists.AsNoTracking()
            .FirstAsync(p => p.Id == PlaylistId.From(forkId));
        Assert.Null(forkEntity.ForkedFromPlaylistId);

        var follows = await discoveryDb.PlaylistFollows
            .Where(f => f.PlaylistId == PlaylistId.From(playlistId))
            .CountAsync();
        Assert.Equal(0, follows);

        var libraryReleases = await client.GetFromJsonAsync<JsonElement>(
            "/api/v1/discovery/library/releases",
            JsonOptions);
        Assert.Contains(
            libraryReleases.GetProperty("releases").EnumerateArray(),
            r => r.GetProperty("releaseId").GetGuid() == releaseId);

        var search = await client.GetAsync($"/api/v1/discovery/search?q=Integration&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, search.StatusCode);
    }

    [Fact]
    public async Task Remove_release_from_playlist_removes_all_tracks_from_that_release()
    {
        using var client = fixture.CreateClient();
        var email = $"playlist-release-{Guid.CreateVersion7():N}@amuse.test";
        const string password = "Password1234!";

        await RegisterConfirmAndLoginListenerAsync(fixture, client, email, password);
        await CompleteOnboardingAsync(client);

        var seededTracks = await GetSeededTracksFromSameReleaseAsync(minCount: 2);
        var releaseId = seededTracks[0].ReleaseId;

        var create = await client.PostAsJsonAsync(
            "/api/v1/discovery/playlists",
            new { title = "Release Removal Playlist", visibility = "private" },
            JsonOptions);
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var playlist = await create.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var playlistId = playlist.GetProperty("id").GetGuid();

        foreach (var track in seededTracks)
        {
            var addTrack = await client.PostAsJsonAsync(
                $"/api/v1/discovery/playlists/{playlistId}/items",
                new { trackId = track.TrackId },
                JsonOptions);
            Assert.Equal(HttpStatusCode.OK, addTrack.StatusCode);
        }

        var otherReleaseTrack = await GetSeededTrackFromDifferentReleaseAsync(releaseId);
        var addOther = await client.PostAsJsonAsync(
            $"/api/v1/discovery/playlists/{playlistId}/items",
            new { trackId = otherReleaseTrack },
            JsonOptions);
        Assert.Equal(HttpStatusCode.OK, addOther.StatusCode);

        var removeRelease = await client.DeleteAsync(
            $"/api/v1/discovery/playlists/{playlistId}/releases/{releaseId}");
        Assert.Equal(HttpStatusCode.NoContent, removeRelease.StatusCode);

        var detail = await client.GetFromJsonAsync<JsonElement>(
            $"/api/v1/discovery/playlists/{playlistId}",
            JsonOptions);
        var items = detail.GetProperty("items").EnumerateArray().ToArray();
        Assert.Single(items);
        Assert.Equal(otherReleaseTrack, items[0].GetProperty("trackId").GetGuid());
    }

    [Fact]
    public async Task Search_is_public_without_authentication()
    {
        using var client = fixture.CreateClient();
        var response = await client.GetAsync("/api/v1/discovery/search?q=test&pageSize=5");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.True(body.TryGetProperty("items", out var items));
        Assert.Equal(JsonValueKind.Array, items.ValueKind);
    }

    [Fact]
    public async Task Search_kinds_filter_returns_only_requested_kind()
    {
        using var client = fixture.CreateClient();
        var response = await client.GetAsync("/api/v1/discovery/search?q=a&pageSize=20&kinds=playlist");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var items = body.GetProperty("items").EnumerateArray().ToArray();
        Assert.All(items, item => Assert.Equal("playlist", item.GetProperty("kind").GetString()));
    }

    private static async Task RegisterConfirmAndLoginListenerAsync(
        AmuseApiFixture fixture,
        HttpClient client,
        string email,
        string password)
    {
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
    }

    private static async Task CompleteOnboardingAsync(HttpClient client)
    {
        var response = await client.PatchAsJsonAsync(
            "/api/v1/listener/profile",
            new { displayName = "Discovery Tester", allowUnverifiedArtists = true },
            JsonOptions);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private async Task<(Guid TrackId, Guid ReleaseId)> GetSeededTrackAndReleaseAsync()
    {
        var tracks = await GetSeededTracksFromSameReleaseAsync(minCount: 1);
        return (tracks[0].TrackId, tracks[0].ReleaseId);
    }

    private async Task<IReadOnlyList<(Guid TrackId, Guid ReleaseId)>> GetSeededTracksFromSameReleaseAsync(
        int minCount)
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var releaseId = await db.Tracks.AsNoTracking()
            .Where(t =>
                t.LifecycleStatus == TrackLifecycleStatus.Published
                && t.AudioMasterKey != null
                && t.AudioMasterKey != "")
            .GroupBy(t => t.ReleaseId)
            .Where(g => g.Count() >= minCount)
            .Select(g => g.Key)
            .FirstAsync();

        var tracks = await db.Tracks.AsNoTracking()
            .Where(t =>
                t.ReleaseId == releaseId
                && t.LifecycleStatus == TrackLifecycleStatus.Published
                && t.AudioMasterKey != null
                && t.AudioMasterKey != "")
            .OrderBy(t => t.TrackNumber)
            .Take(minCount)
            .Select(t => new { TrackId = t.Id.Value, ReleaseId = t.ReleaseId.Value })
            .ToListAsync();

        return tracks.Select(t => (t.TrackId, t.ReleaseId)).ToArray();
    }

    private async Task<Guid> GetSeededTrackFromDifferentReleaseAsync(Guid excludedReleaseId)
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        return await db.Tracks.AsNoTracking()
            .Where(t =>
                t.ReleaseId != ReleaseId.From(excludedReleaseId)
                && t.LifecycleStatus == TrackLifecycleStatus.Published
                && t.AudioMasterKey != null
                && t.AudioMasterKey != "")
            .Select(t => t.Id.Value)
            .FirstAsync();
    }
}
