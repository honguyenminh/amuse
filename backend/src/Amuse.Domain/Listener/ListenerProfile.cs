using Amuse.Domain.Identity;

namespace Amuse.Domain.Listener;

public sealed class ListenerProfile
{
    public ListenerProfileId Id { get; private set; }
    public AccountId AccountId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private ListenerProfile()
    {
    }

    private ListenerProfile(ListenerProfileId id, AccountId accountId, DateTimeOffset createdAt)
    {
        Id = id;
        AccountId = accountId;
        CreatedAt = createdAt;
    }

    public static ListenerProfile Create(AccountId accountId, DateTimeOffset createdAt) =>
        new(ListenerProfileId.New(), accountId, createdAt);
}
