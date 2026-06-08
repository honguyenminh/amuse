using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Amuse.Domain.Catalog;
using Amuse.Domain.Tenancy;
using Amuse.Modules.Catalog.Features.Common;
using Amuse.Modules.Identity.Features.Common;
using Amuse.Modules.Tenancy.Features.Common;
using Microsoft.AspNetCore.WebUtilities;

namespace Amuse.Api.IntegrationTests;

[Collection(AmuseApiCollection.Name)]
public sealed class TenancyMemberFlowTests(AmuseApiFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    [Fact]
    public async Task Invite_accept_and_org_persona_includes_member_claims()
    {
        fixture.CaptureEmailSender.Reset();
        using var client = fixture.CreateClient();
        var ownerTokens = await LoginAsync(client, "root@amuse.local", "ChangeMe_Root123!");

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", ownerTokens.AccessToken);

        var create = await client.PostAsJsonAsync(
            "/api/v1/tenancy/organizations",
            new { displayName = "Member Flow Org", orgClass = OrganizationClass.IndieGroup },
            JsonOptions);
        create.EnsureSuccessStatusCode();
        var org = (await create.Content.ReadFromJsonAsync<OrganizationResponse>(JsonOptions))!;

        var orgTokens = await RefreshOrgPersonaAsync(client, ownerTokens.RefreshToken!, org.Id);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", orgTokens.AccessToken);

        var inviteeEmail = $"member-{Guid.CreateVersion7():N}@amuse.test";
        var invite = await client.PostAsJsonAsync(
            $"/api/v1/tenancy/organizations/{org.Id}/members/invites",
            new
            {
                email = inviteeEmail,
                presetRoleLabel = OrgClaimPresets.MemberManagerPresetLabel,
                claims = (string[]?)null,
            },
            JsonOptions);
        Assert.Equal(HttpStatusCode.Created, invite.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(fixture.CaptureEmailSender.LastInviteUrl));

        var inviteUri = new Uri(fixture.CaptureEmailSender.LastInviteUrl!);
        var inviteQuery = QueryHelpers.ParseQuery(inviteUri.Query);
        var inviteToken = inviteQuery["token"].ToString();
        Assert.False(string.IsNullOrWhiteSpace(inviteToken));

        const string inviteePassword = "Password1234!";
        var register = await client.PostAsJsonAsync(
            "/api/v1/identity/register/password",
            new { email = inviteeEmail, password = inviteePassword, portal = "business" },
            JsonOptions);
        Assert.Equal(HttpStatusCode.Accepted, register.StatusCode);

        var confirmUri = new Uri(fixture.CaptureEmailSender.LastConfirmUrl!);
        var confirmQuery = QueryHelpers.ParseQuery(confirmUri.Query);
        var confirm = await client.PostAsJsonAsync(
            "/api/v1/identity/confirm-email",
            new
            {
                userId = Guid.Parse(confirmQuery["userId"]!),
                token = Uri.UnescapeDataString(confirmQuery["token"]!),
            },
            JsonOptions);
        Assert.Equal(HttpStatusCode.NoContent, confirm.StatusCode);

        var inviteeLogin = await LoginAsListenerAsync(client, inviteeEmail, inviteePassword);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", inviteeLogin.AccessToken);

        var accept = await client.PostAsync(
            $"/api/v1/tenancy/invites/{Uri.EscapeDataString(inviteToken)}/accept",
            null);
        Assert.Equal(HttpStatusCode.OK, accept.StatusCode);

        var memberOrgTokens = await RefreshOrgPersonaAsync(
            client,
            inviteeLogin.RefreshToken!,
            org.Id);

        var jwtClaims = ReadJwtClaims(memberOrgTokens.AccessToken);
        Assert.Contains("read:membership:all", jwtClaims);
        Assert.Contains("manage:membership:all", jwtClaims);
    }

    [Fact]
    public async Task Admin_preset_invite_succeeds_for_active_indie_group()
    {
        fixture.CaptureEmailSender.Reset();
        using var client = fixture.CreateClient();
        var ownerTokens = await LoginAsync(client, "root@amuse.local", "ChangeMe_Root123!");

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", ownerTokens.AccessToken);

        var create = await client.PostAsJsonAsync(
            "/api/v1/tenancy/organizations",
            new { displayName = "Admin Invite Org", orgClass = OrganizationClass.IndieGroup },
            JsonOptions);
        create.EnsureSuccessStatusCode();
        var org = (await create.Content.ReadFromJsonAsync<OrganizationResponse>(JsonOptions))!;

        var orgTokens = await RefreshOrgPersonaAsync(client, ownerTokens.RefreshToken!, org.Id);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", orgTokens.AccessToken);

        var invite = await client.PostAsJsonAsync(
            $"/api/v1/tenancy/organizations/{org.Id}/members/invites",
            new
            {
                email = $"admin-{Guid.CreateVersion7():N}@amuse.test",
                presetRoleLabel = OrgClaimPresets.OwnerPresetLabel,
                claims = (string[]?)null,
            },
            JsonOptions);
        Assert.Equal(HttpStatusCode.Created, invite.StatusCode);
    }

    [Fact]
    public async Task Owner_member_cannot_be_removed()
    {
        using var client = fixture.CreateClient();
        var ownerTokens = await LoginAsync(client, "root@amuse.local", "ChangeMe_Root123!");

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", ownerTokens.AccessToken);

        var create = await client.PostAsJsonAsync(
            "/api/v1/tenancy/organizations",
            new { displayName = "Owner Protected Org", orgClass = OrganizationClass.IndieGroup },
            JsonOptions);
        create.EnsureSuccessStatusCode();
        var org = (await create.Content.ReadFromJsonAsync<OrganizationResponse>(JsonOptions))!;

        var orgTokens = await RefreshOrgPersonaAsync(client, ownerTokens.RefreshToken!, org.Id);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", orgTokens.AccessToken);

        var members = await client.GetAsync($"/api/v1/tenancy/organizations/{org.Id}/members");
        members.EnsureSuccessStatusCode();
        var memberList = await members.Content.ReadFromJsonAsync<OrganizationMemberListResponse>(JsonOptions);
        Assert.NotNull(memberList);
        var ownerMember = memberList.Items.First(m => m.IsOwner);

        var remove = await client.DeleteAsync(
            $"/api/v1/tenancy/organizations/{org.Id}/members/{ownerMember.Id}");
        Assert.Equal(HttpStatusCode.BadRequest, remove.StatusCode);
    }

    [Fact]
    public async Task Removed_member_can_accept_new_invite_and_rejoin()
    {
        fixture.CaptureEmailSender.Reset();
        using var client = fixture.CreateClient();
        var ownerTokens = await LoginAsync(client, "root@amuse.local", "ChangeMe_Root123!");

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", ownerTokens.AccessToken);

        var create = await client.PostAsJsonAsync(
            "/api/v1/tenancy/organizations",
            new { displayName = "Rejoin Flow Org", orgClass = OrganizationClass.IndieGroup },
            JsonOptions);
        create.EnsureSuccessStatusCode();
        var org = (await create.Content.ReadFromJsonAsync<OrganizationResponse>(JsonOptions))!;

        var orgTokens = await RefreshOrgPersonaAsync(client, ownerTokens.RefreshToken!, org.Id);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", orgTokens.AccessToken);

        var inviteeEmail = $"rejoin-{Guid.CreateVersion7():N}@amuse.test";
        var invite = await client.PostAsJsonAsync(
            $"/api/v1/tenancy/organizations/{org.Id}/members/invites",
            new
            {
                email = inviteeEmail,
                presetRoleLabel = OrgClaimPresets.ViewerPresetLabel,
                claims = (string[]?)null,
            },
            JsonOptions);
        Assert.Equal(HttpStatusCode.Created, invite.StatusCode);

        var firstInviteUri = new Uri(fixture.CaptureEmailSender.LastInviteUrl!);
        var firstInviteToken = QueryHelpers.ParseQuery(firstInviteUri.Query)["token"].ToString();

        const string inviteePassword = "Password1234!";
        await client.PostAsJsonAsync(
            "/api/v1/identity/register/password",
            new { email = inviteeEmail, password = inviteePassword, portal = "business" },
            JsonOptions);

        var confirmUri = new Uri(fixture.CaptureEmailSender.LastConfirmUrl!);
        var confirmQuery = QueryHelpers.ParseQuery(confirmUri.Query);
        await client.PostAsJsonAsync(
            "/api/v1/identity/confirm-email",
            new
            {
                userId = Guid.Parse(confirmQuery["userId"]!),
                token = Uri.UnescapeDataString(confirmQuery["token"]!),
            },
            JsonOptions);

        var inviteeLogin = await LoginAsListenerAsync(client, inviteeEmail, inviteePassword);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", inviteeLogin.AccessToken);
        var firstAccept = await client.PostAsync(
            $"/api/v1/tenancy/invites/{Uri.EscapeDataString(firstInviteToken)}/accept",
            null);
        Assert.Equal(HttpStatusCode.OK, firstAccept.StatusCode);
        var firstAcceptBody = await firstAccept.Content.ReadFromJsonAsync<AcceptInviteResponse>(JsonOptions);
        Assert.NotNull(firstAcceptBody);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", orgTokens.AccessToken);
        var members = await client.GetAsync($"/api/v1/tenancy/organizations/{org.Id}/members");
        members.EnsureSuccessStatusCode();
        var memberList = await members.Content.ReadFromJsonAsync<OrganizationMemberListResponse>(JsonOptions);
        Assert.NotNull(memberList);
        var joinedMember = memberList.Items.First(m => m.Email == inviteeEmail);

        var remove = await client.DeleteAsync(
            $"/api/v1/tenancy/organizations/{org.Id}/members/{joinedMember.Id}");
        remove.EnsureSuccessStatusCode();

        fixture.CaptureEmailSender.Reset();
        var secondInvite = await client.PostAsJsonAsync(
            $"/api/v1/tenancy/organizations/{org.Id}/members/invites",
            new
            {
                email = inviteeEmail,
                presetRoleLabel = OrgClaimPresets.MemberManagerPresetLabel,
                claims = (string[]?)null,
            },
            JsonOptions);
        Assert.Equal(HttpStatusCode.Created, secondInvite.StatusCode);

        var secondInviteUri = new Uri(fixture.CaptureEmailSender.LastInviteUrl!);
        var secondInviteToken = QueryHelpers.ParseQuery(secondInviteUri.Query)["token"].ToString();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", inviteeLogin.AccessToken);
        var secondAccept = await client.PostAsync(
            $"/api/v1/tenancy/invites/{Uri.EscapeDataString(secondInviteToken)}/accept",
            null);
        Assert.Equal(HttpStatusCode.OK, secondAccept.StatusCode);
        var secondAcceptBody = await secondAccept.Content.ReadFromJsonAsync<AcceptInviteResponse>(JsonOptions);
        Assert.NotNull(secondAcceptBody);
        Assert.Equal(firstAcceptBody.MemberId, secondAcceptBody.MemberId);

        var memberOrgTokens = await RefreshOrgPersonaAsync(client, inviteeLogin.RefreshToken!, org.Id);
        var jwtClaims = ReadJwtClaims(memberOrgTokens.AccessToken);
        Assert.Contains("manage:membership:all", jwtClaims);
    }

    [Fact]
    public async Task Non_owner_member_can_leave_organization()
    {
        fixture.CaptureEmailSender.Reset();
        using var client = fixture.CreateClient();
        var ownerTokens = await LoginAsync(client, "root@amuse.local", "ChangeMe_Root123!");

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", ownerTokens.AccessToken);

        var create = await client.PostAsJsonAsync(
            "/api/v1/tenancy/organizations",
            new { displayName = "Leave Flow Org", orgClass = OrganizationClass.IndieGroup },
            JsonOptions);
        create.EnsureSuccessStatusCode();
        var org = (await create.Content.ReadFromJsonAsync<OrganizationResponse>(JsonOptions))!;

        var orgTokens = await RefreshOrgPersonaAsync(client, ownerTokens.RefreshToken!, org.Id);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", orgTokens.AccessToken);

        var inviteeEmail = $"leave-{Guid.CreateVersion7():N}@amuse.test";
        var invite = await client.PostAsJsonAsync(
            $"/api/v1/tenancy/organizations/{org.Id}/members/invites",
            new
            {
                email = inviteeEmail,
                presetRoleLabel = OrgClaimPresets.ViewerPresetLabel,
                claims = (string[]?)null,
            },
            JsonOptions);
        Assert.Equal(HttpStatusCode.Created, invite.StatusCode);

        var inviteUri = new Uri(fixture.CaptureEmailSender.LastInviteUrl!);
        var inviteToken = QueryHelpers.ParseQuery(inviteUri.Query)["token"].ToString();

        const string inviteePassword = "Password1234!";
        await client.PostAsJsonAsync(
            "/api/v1/identity/register/password",
            new { email = inviteeEmail, password = inviteePassword, portal = "business" },
            JsonOptions);

        var confirmUri = new Uri(fixture.CaptureEmailSender.LastConfirmUrl!);
        var confirmQuery = QueryHelpers.ParseQuery(confirmUri.Query);
        await client.PostAsJsonAsync(
            "/api/v1/identity/confirm-email",
            new
            {
                userId = Guid.Parse(confirmQuery["userId"]!),
                token = Uri.UnescapeDataString(confirmQuery["token"]!),
            },
            JsonOptions);

        var inviteeLogin = await LoginAsListenerAsync(client, inviteeEmail, inviteePassword);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", inviteeLogin.AccessToken);
        await client.PostAsync(
            $"/api/v1/tenancy/invites/{Uri.EscapeDataString(inviteToken)}/accept",
            null);

        var memberOrgTokens = await RefreshOrgPersonaAsync(client, inviteeLogin.RefreshToken!, org.Id);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", memberOrgTokens.AccessToken);

        var leave = await client.PostAsync(
            $"/api/v1/tenancy/organizations/{org.Id}/membership/leave",
            null);
        Assert.Equal(HttpStatusCode.NoContent, leave.StatusCode);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", orgTokens.AccessToken);
        var members = await client.GetAsync($"/api/v1/tenancy/organizations/{org.Id}/members");
        members.EnsureSuccessStatusCode();
        var memberList = await members.Content.ReadFromJsonAsync<OrganizationMemberListResponse>(JsonOptions);
        Assert.NotNull(memberList);
        Assert.DoesNotContain(memberList.Items, m => m.Email == inviteeEmail);
    }

    [Fact]
    public async Task Owner_cannot_leave_organization()
    {
        using var client = fixture.CreateClient();
        var ownerTokens = await LoginAsync(client, "root@amuse.local", "ChangeMe_Root123!");

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", ownerTokens.AccessToken);

        var create = await client.PostAsJsonAsync(
            "/api/v1/tenancy/organizations",
            new { displayName = "Owner Leave Blocked Org", orgClass = OrganizationClass.IndieGroup },
            JsonOptions);
        create.EnsureSuccessStatusCode();
        var org = (await create.Content.ReadFromJsonAsync<OrganizationResponse>(JsonOptions))!;

        var orgTokens = await RefreshOrgPersonaAsync(client, ownerTokens.RefreshToken!, org.Id);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", orgTokens.AccessToken);

        var leave = await client.PostAsync(
            $"/api/v1/tenancy/organizations/{org.Id}/membership/leave",
            null);
        Assert.Equal(HttpStatusCode.BadRequest, leave.StatusCode);
    }

    [Fact]
    public async Task Member_cannot_remove_themselves()
    {
        fixture.CaptureEmailSender.Reset();
        using var client = fixture.CreateClient();
        var ownerTokens = await LoginAsync(client, "root@amuse.local", "ChangeMe_Root123!");

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", ownerTokens.AccessToken);

        var create = await client.PostAsJsonAsync(
            "/api/v1/tenancy/organizations",
            new { displayName = "Self Remove Guard Org", orgClass = OrganizationClass.IndieGroup },
            JsonOptions);
        create.EnsureSuccessStatusCode();
        var org = (await create.Content.ReadFromJsonAsync<OrganizationResponse>(JsonOptions))!;

        var orgTokens = await RefreshOrgPersonaAsync(client, ownerTokens.RefreshToken!, org.Id);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", orgTokens.AccessToken);

        var inviteeEmail = $"self-remove-{Guid.CreateVersion7():N}@amuse.test";
        var invite = await client.PostAsJsonAsync(
            $"/api/v1/tenancy/organizations/{org.Id}/members/invites",
            new
            {
                email = inviteeEmail,
                presetRoleLabel = OrgClaimPresets.MemberManagerPresetLabel,
                claims = (string[]?)null,
            },
            JsonOptions);
        Assert.Equal(HttpStatusCode.Created, invite.StatusCode);

        var inviteUri = new Uri(fixture.CaptureEmailSender.LastInviteUrl!);
        var inviteToken = QueryHelpers.ParseQuery(inviteUri.Query)["token"].ToString();

        const string inviteePassword = "Password1234!";
        await client.PostAsJsonAsync(
            "/api/v1/identity/register/password",
            new { email = inviteeEmail, password = inviteePassword, portal = "business" },
            JsonOptions);

        var confirmUri = new Uri(fixture.CaptureEmailSender.LastConfirmUrl!);
        var confirmQuery = QueryHelpers.ParseQuery(confirmUri.Query);
        await client.PostAsJsonAsync(
            "/api/v1/identity/confirm-email",
            new
            {
                userId = Guid.Parse(confirmQuery["userId"]!),
                token = Uri.UnescapeDataString(confirmQuery["token"]!),
            },
            JsonOptions);

        var inviteeLogin = await LoginAsListenerAsync(client, inviteeEmail, inviteePassword);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", inviteeLogin.AccessToken);
        await client.PostAsync(
            $"/api/v1/tenancy/invites/{Uri.EscapeDataString(inviteToken)}/accept",
            null);

        var managerTokens = await RefreshOrgPersonaAsync(client, inviteeLogin.RefreshToken!, org.Id);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", managerTokens.AccessToken);

        var members = await client.GetAsync($"/api/v1/tenancy/organizations/{org.Id}/members");
        members.EnsureSuccessStatusCode();
        var memberList = await members.Content.ReadFromJsonAsync<OrganizationMemberListResponse>(JsonOptions);
        Assert.NotNull(memberList);
        var self = memberList.Items.First(m => m.Email == inviteeEmail);

        var remove = await client.DeleteAsync(
            $"/api/v1/tenancy/organizations/{org.Id}/members/{self.Id}");
        Assert.Equal(HttpStatusCode.BadRequest, remove.StatusCode);
    }

    [Fact]
    public async Task Member_manager_cannot_update_member_permissions()
    {
        fixture.CaptureEmailSender.Reset();
        using var client = fixture.CreateClient();
        var ownerTokens = await LoginAsync(client, "root@amuse.local", "ChangeMe_Root123!");

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", ownerTokens.AccessToken);

        var create = await client.PostAsJsonAsync(
            "/api/v1/tenancy/organizations",
            new { displayName = "Permissions Gate Org", orgClass = OrganizationClass.IndieGroup },
            JsonOptions);
        create.EnsureSuccessStatusCode();
        var org = (await create.Content.ReadFromJsonAsync<OrganizationResponse>(JsonOptions))!;

        var orgTokens = await RefreshOrgPersonaAsync(client, ownerTokens.RefreshToken!, org.Id);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", orgTokens.AccessToken);

        var inviteeEmail = $"perm-gate-{Guid.CreateVersion7():N}@amuse.test";
        var invite = await client.PostAsJsonAsync(
            $"/api/v1/tenancy/organizations/{org.Id}/members/invites",
            new
            {
                email = inviteeEmail,
                presetRoleLabel = OrgClaimPresets.MemberManagerPresetLabel,
                claims = (string[]?)null,
            },
            JsonOptions);
        Assert.Equal(HttpStatusCode.Created, invite.StatusCode);

        var inviteUri = new Uri(fixture.CaptureEmailSender.LastInviteUrl!);
        var inviteToken = QueryHelpers.ParseQuery(inviteUri.Query)["token"].ToString();

        const string inviteePassword = "Password1234!";
        await client.PostAsJsonAsync(
            "/api/v1/identity/register/password",
            new { email = inviteeEmail, password = inviteePassword, portal = "business" },
            JsonOptions);

        var confirmUri = new Uri(fixture.CaptureEmailSender.LastConfirmUrl!);
        var confirmQuery = QueryHelpers.ParseQuery(confirmUri.Query);
        await client.PostAsJsonAsync(
            "/api/v1/identity/confirm-email",
            new
            {
                userId = Guid.Parse(confirmQuery["userId"]!),
                token = Uri.UnescapeDataString(confirmQuery["token"]!),
            },
            JsonOptions);

        var inviteeLogin = await LoginAsListenerAsync(client, inviteeEmail, inviteePassword);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", inviteeLogin.AccessToken);
        await client.PostAsync(
            $"/api/v1/tenancy/invites/{Uri.EscapeDataString(inviteToken)}/accept",
            null);

        var managerTokens = await RefreshOrgPersonaAsync(client, inviteeLogin.RefreshToken!, org.Id);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", managerTokens.AccessToken);

        var members = await client.GetAsync($"/api/v1/tenancy/organizations/{org.Id}/members");
        members.EnsureSuccessStatusCode();
        var memberList = await members.Content.ReadFromJsonAsync<OrganizationMemberListResponse>(JsonOptions);
        Assert.NotNull(memberList);
        var managerMember = memberList.Items.First(m => !m.IsOwner);

        var update = await client.PatchAsJsonAsync(
            $"/api/v1/tenancy/organizations/{org.Id}/members/{managerMember.Id}",
            new { presetRoleLabel = OrgClaimPresets.ViewerPresetLabel, claims = (string[]?)null },
            JsonOptions);
        Assert.Equal(HttpStatusCode.Forbidden, update.StatusCode);
    }

    [Fact]
    public async Task Invite_with_explicit_per_resource_catalog_claims_grants_scoped_access()
    {
        fixture.CaptureEmailSender.Reset();
        using var client = fixture.CreateClient();
        var ownerTokens = await LoginAsync(client, "root@amuse.local", "ChangeMe_Root123!");

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", ownerTokens.AccessToken);

        var create = await client.PostAsJsonAsync(
            "/api/v1/tenancy/organizations",
            new
            {
                displayName = "Scoped Catalog Org",
                orgClass = OrganizationClass.IndieGroup,
                createDefaultArtist = true,
            },
            JsonOptions);
        create.EnsureSuccessStatusCode();
        var org = (await create.Content.ReadFromJsonAsync<OrganizationResponse>(JsonOptions))!;

        var orgTokens = await RefreshOrgPersonaAsync(client, ownerTokens.RefreshToken!, org.Id);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", orgTokens.AccessToken);

        var artists = await client.GetFromJsonAsync<ManageArtistListResponse>(
            "/api/v1/catalog/manage/artists",
            JsonOptions);
        Assert.NotNull(artists);
        Assert.NotEmpty(artists.Items);
        var allowedArtistId = artists.Items[0].Id;

        var secondArtist = await client.PostAsJsonAsync(
            "/api/v1/catalog/artists",
            new
            {
                name = "Second Roster Artist",
                slug = $"second-{Guid.CreateVersion7():N}",
            },
            JsonOptions);
        secondArtist.EnsureSuccessStatusCode();
        var deniedArtist = await secondArtist.Content.ReadFromJsonAsync<ManageArtistSummaryResponse>(JsonOptions);
        Assert.NotNull(deniedArtist);

        var createRelease = await client.PostAsJsonAsync(
            $"/api/v1/catalog/artists/{allowedArtistId}/releases",
            new
            {
                title = "Scoped Reader Single",
                releaseType = ReleaseType.Single,
                releaseDate = "2026-06-01T00:00:00Z",
                metadataComplete = true,
            },
            JsonOptions);
        createRelease.EnsureSuccessStatusCode();

        var scopedClaims = new[]
        {
            "read:org:all",
            $"read:catalog:artist:{allowedArtistId:D}",
        };

        var inviteeEmail = $"scoped-catalog-{Guid.CreateVersion7():N}@amuse.test";
        var invite = await client.PostAsJsonAsync(
            $"/api/v1/tenancy/organizations/{org.Id}/members/invites",
            new
            {
                email = inviteeEmail,
                presetRoleLabel = (string?)null,
                claims = scopedClaims,
            },
            JsonOptions);
        Assert.Equal(HttpStatusCode.Created, invite.StatusCode);

        var inviteUri = new Uri(fixture.CaptureEmailSender.LastInviteUrl!);
        var inviteToken = QueryHelpers.ParseQuery(inviteUri.Query)["token"].ToString();

        const string inviteePassword = "Password1234!";
        await client.PostAsJsonAsync(
            "/api/v1/identity/register/password",
            new { email = inviteeEmail, password = inviteePassword, portal = "business" },
            JsonOptions);

        var confirmUri = new Uri(fixture.CaptureEmailSender.LastConfirmUrl!);
        var confirmQuery = QueryHelpers.ParseQuery(confirmUri.Query);
        await client.PostAsJsonAsync(
            "/api/v1/identity/confirm-email",
            new
            {
                userId = Guid.Parse(confirmQuery["userId"]!),
                token = Uri.UnescapeDataString(confirmQuery["token"]!),
            },
            JsonOptions);

        var inviteeLogin = await LoginAsListenerAsync(client, inviteeEmail, inviteePassword);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", inviteeLogin.AccessToken);
        await client.PostAsync(
            $"/api/v1/tenancy/invites/{Uri.EscapeDataString(inviteToken)}/accept",
            null);

        var memberOrgTokens = await RefreshOrgPersonaAsync(
            client,
            inviteeLogin.RefreshToken!,
            org.Id);

        var jwtClaims = ReadJwtClaims(memberOrgTokens.AccessToken);
        Assert.Contains($"read:catalog:artist:{allowedArtistId:D}", jwtClaims);
        Assert.DoesNotContain("read:payout:all", jwtClaims);
        Assert.DoesNotContain("upload:catalog:all", jwtClaims);
        Assert.DoesNotContain("write_draft:catalog:all", jwtClaims);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", memberOrgTokens.AccessToken);

        var balance = await client.GetAsync("/api/v1/billing/balance");
        Assert.Equal(HttpStatusCode.Forbidden, balance.StatusCode);

        var coverPresign = await client.PostAsJsonAsync(
            $"/api/v1/catalog/artists/{allowedArtistId}/cover/presign-upload",
            new { fileName = "cover.jpg", contentType = "image/jpeg" },
            JsonOptions);
        Assert.Equal(HttpStatusCode.Forbidden, coverPresign.StatusCode);

        var visibleArtists = await client.GetFromJsonAsync<ManageArtistListResponse>(
            "/api/v1/catalog/manage/artists",
            JsonOptions);
        Assert.NotNull(visibleArtists);
        Assert.Single(visibleArtists.Items);
        Assert.Equal(allowedArtistId, visibleArtists.Items[0].Id);

        var allowedDetail = await client.GetFromJsonAsync<ManageArtistDetailResponse>(
            $"/api/v1/catalog/manage/artists/{allowedArtistId}",
            JsonOptions);
        Assert.NotNull(allowedDetail);
        Assert.Single(allowedDetail.Releases);
        Assert.Equal("Scoped Reader Single", allowedDetail.Releases[0].Title);
        Assert.NotEmpty(allowedDetail.ReleaseGroups);

        var deniedDetail = await client.GetAsync(
            $"/api/v1/catalog/manage/artists/{deniedArtist.Id}");
        Assert.Equal(HttpStatusCode.BadRequest, deniedDetail.StatusCode);
    }

    private static IReadOnlyList<string> ReadJwtClaims(string accessToken)
    {
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(accessToken);
        return jwt.Claims.Where(c => c.Type == "claims").Select(c => c.Value).ToList();
    }

    private static async Task<AuthTokenResponse> LoginAsync(
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
                context = new { type = "platform", orgId = (Guid?)null, listenerId = (Guid?)null },
            },
            JsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthTokenResponse>(JsonOptions))!;
    }

    private static async Task<AuthTokenResponse> LoginAsListenerAsync(
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
