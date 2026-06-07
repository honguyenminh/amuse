using Amuse.Domain.Identity;
using Amuse.Domain.Tenancy;

namespace Amuse.Domain.Tests.Tenancy;

public sealed class OrganizationInviteTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-06-01T12:00:00Z");

    [Fact]
    public void IsExpired_true_when_pending_and_past_expires_at()
    {
        var invite = CreatePendingInvite(Now.AddDays(-1));
        Assert.True(invite.IsExpired(Now));
    }

    [Fact]
    public void EnsurePending_marks_expired_when_past_ttl()
    {
        var invite = CreatePendingInvite(Now.AddDays(-1));
        var result = invite.EnsurePending(Now);
        Assert.False(result.IsSuccess);
        Assert.Equal(OrganizationInviteStatus.Expired, invite.Status);
    }

    [Fact]
    public void Decline_revokes_pending_invite()
    {
        var invite = CreatePendingInvite(Now.AddDays(1));
        var result = invite.Decline(Now);
        Assert.True(result.IsSuccess);
        Assert.Equal(OrganizationInviteStatus.Revoked, invite.Status);
    }

    private static OrganizationInvite CreatePendingInvite(DateTimeOffset expiresAt)
    {
        var create = OrganizationInvite.CreatePending(
            OrganizationId.New(),
            "invitee@example.com",
            AccountId.New(),
            "admin",
            ["manage:org"],
            Now);
        Assert.True(create.IsSuccess);
        var invite = create.Value!.Invite;
        typeof(OrganizationInvite).GetProperty(nameof(OrganizationInvite.ExpiresAt))!
            .SetValue(invite, expiresAt);
        return invite;
    }
}
