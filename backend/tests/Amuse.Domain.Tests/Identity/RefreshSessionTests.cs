using Amuse.Domain.Identity;

namespace Amuse.Domain.Tests.Identity;

public sealed class RefreshSessionTests
{
    [Fact]
    public void IsActive_false_when_revoked()
    {
        var session = RefreshSession.Create(
            AccountId.New(),
            "hash",
            DateTimeOffset.UtcNow.AddDays(1),
            DateTimeOffset.UtcNow);

        session.Revoke(DateTimeOffset.UtcNow);
        Assert.False(session.IsActive(DateTimeOffset.UtcNow));
    }
}
