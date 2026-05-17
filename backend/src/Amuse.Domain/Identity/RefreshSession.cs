namespace Amuse.Domain.Identity;

public sealed class RefreshSession
{
    public RefreshSessionId Id { get; private set; }
    public AccountId AccountId { get; private set; }
    public string TokenHash { get; private set; } = null!;
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }

    private RefreshSession()
    {
    }

    private RefreshSession(
        RefreshSessionId id,
        AccountId accountId,
        string tokenHash,
        DateTimeOffset expiresAt,
        DateTimeOffset createdAt)
    {
        Id = id;
        AccountId = accountId;
        TokenHash = tokenHash;
        ExpiresAt = expiresAt;
        CreatedAt = createdAt;
    }

    public static RefreshSession Create(
        AccountId accountId,
        string tokenHash,
        DateTimeOffset expiresAt,
        DateTimeOffset createdAt) =>
        new(RefreshSessionId.New(), accountId, tokenHash, expiresAt, createdAt);

    public bool IsActive(DateTimeOffset now) =>
        RevokedAt is null && ExpiresAt > now;

    public void Revoke(DateTimeOffset revokedAt) => RevokedAt = revokedAt;
}
