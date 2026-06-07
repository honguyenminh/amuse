using System.Collections.Concurrent;

namespace Amuse.Modules.Identity.Auth;

internal sealed class JwtBlacklistLocalCache
{
    private readonly ConcurrentDictionary<string, DateTimeOffset> _entries = new();

    public bool IsRevoked(string jti, DateTimeOffset now)
    {
        if (!_entries.TryGetValue(jti, out var expiresAt))
            return false;

        if (expiresAt > now)
            return true;

        _entries.TryRemove(jti, out _);
        return false;
    }

    public void Remember(string jti, DateTimeOffset expiresAt) =>
        _entries[jti] = expiresAt;
}
