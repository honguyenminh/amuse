namespace Amuse.Domain.Identity;

public sealed class TokenBlacklistEntry
{
    public TokenJti Jti { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public string? Reason { get; private set; }

    private TokenBlacklistEntry()
    {
    }

    private TokenBlacklistEntry(TokenJti jti, DateTimeOffset expiresAt, string? reason)
    {
        Jti = jti;
        ExpiresAt = expiresAt;
        Reason = reason;
    }

    public static TokenBlacklistEntry Create(TokenJti jti, DateTimeOffset expiresAt, string? reason = null) =>
        new(jti, expiresAt, reason);
}
