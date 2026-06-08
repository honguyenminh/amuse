using Amuse.Domain.SharedKernel;

namespace Amuse.Domain.Identity;

public sealed class Account
{
    public AccountId Id { get; private set; }

    public IdpIssuer IdpIssuer { get; private set; } = null!;

    public IdpSubject IdpSubject { get; private set; } = null!;

    public AccountStatus Status { get; private set; }

    public DateTimeOffset? BannedAt { get; private set; }

    private Account()
    {
    }

    private Account(
        AccountId id,
        IdpIssuer idpIssuer,
        IdpSubject idpSubject,
        AccountStatus status,
        DateTimeOffset? bannedAt)
    {
        Id = id;
        IdpIssuer = idpIssuer;
        IdpSubject = idpSubject;
        Status = status;
        BannedAt = bannedAt;
    }

    public static Account Create(IdpIssuer idpIssuer, IdpSubject idpSubject) =>
        new(AccountId.New(), idpIssuer, idpSubject, AccountStatus.Enabled, null);

    public static Account CreateWithId(AccountId id, IdpIssuer idpIssuer, IdpSubject idpSubject) =>
        new(id, idpIssuer, idpSubject, AccountStatus.Enabled, null);

    public bool IsEnabled => Status == AccountStatus.Enabled;

    public bool IsBanned => Status == AccountStatus.Banned;

    public Result Ban(DateTimeOffset now)
    {
        if (Status == AccountStatus.Banned)
            return Result.Failure(IdentityErrors.AccountBanned);

        Status = AccountStatus.Banned;
        BannedAt = now;
        return Result.Success();
    }
}
