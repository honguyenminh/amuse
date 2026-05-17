namespace Amuse.Domain.Identity;

public sealed class Account
{
    public AccountId Id { get; private set; }

    public IdpIssuer IdpIssuer { get; private set; } = null!;

    public IdpSubject IdpSubject { get; private set; } = null!;

    public AccountStatus Status { get; private set; }

    private Account()
    {
    }

    private Account(AccountId id, IdpIssuer idpIssuer, IdpSubject idpSubject, AccountStatus status)
    {
        Id = id;
        IdpIssuer = idpIssuer;
        IdpSubject = idpSubject;
        Status = status;
    }

    public static Account Create(IdpIssuer idpIssuer, IdpSubject idpSubject) =>
        new(AccountId.New(), idpIssuer, idpSubject, AccountStatus.Enabled);

    public static Account CreateWithId(AccountId id, IdpIssuer idpIssuer, IdpSubject idpSubject) =>
        new(id, idpIssuer, idpSubject, AccountStatus.Enabled);

    public bool IsEnabled => Status == AccountStatus.Enabled;
}
