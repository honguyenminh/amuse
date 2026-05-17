using Amuse.Domain.Identity;

namespace Amuse.Domain.Platform;

public sealed class PlatformOperator
{
    public PlatformOperatorId Id { get; private set; }
    public AccountId AccountId { get; private set; }
    public IReadOnlyList<string> Claims { get; private set; } = [];
    public DateTimeOffset CreatedAt { get; private set; }

    public bool IsRoot => Id == PlatformOperatorId.Root;

    private PlatformOperator()
    {
    }

    private PlatformOperator(
        PlatformOperatorId id,
        AccountId accountId,
        IReadOnlyList<string> claims,
        DateTimeOffset createdAt)
    {
        Id = id;
        AccountId = accountId;
        Claims = claims;
        CreatedAt = createdAt;
    }

    public static PlatformOperator Create(
        PlatformOperatorId id,
        AccountId accountId,
        IReadOnlyList<string> claims,
        DateTimeOffset createdAt) =>
        new(id, accountId, claims, createdAt);
}
