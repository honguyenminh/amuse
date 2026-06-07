using Amuse.Modules.Identity.Auth;

namespace Amuse.Modules.Identity.Tests;

public sealed class JwtBlacklistLocalCacheTests
{
    [Fact]
    public void IsRevoked_returns_true_before_expiry()
    {
        var cache = new JwtBlacklistLocalCache();
        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.AddMinutes(10);

        cache.Remember("jti-1", expiresAt);

        Assert.True(cache.IsRevoked("jti-1", now));
    }

    [Fact]
    public void IsRevoked_returns_false_after_expiry_and_removes_entry()
    {
        var cache = new JwtBlacklistLocalCache();
        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.AddMinutes(-1);

        cache.Remember("jti-1", expiresAt);

        Assert.False(cache.IsRevoked("jti-1", now));
        Assert.False(cache.IsRevoked("jti-1", now));
    }

    [Fact]
    public void IsRevoked_returns_false_for_unknown_jti()
    {
        var cache = new JwtBlacklistLocalCache();

        Assert.False(cache.IsRevoked("missing", DateTimeOffset.UtcNow));
    }
}
