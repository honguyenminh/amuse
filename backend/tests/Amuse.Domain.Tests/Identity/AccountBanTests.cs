using Amuse.Domain.Identity;

namespace Amuse.Domain.Tests.Identity;

public sealed class AccountBanTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-06-08T00:00:00+00:00");

    [Fact]
    public void Ban_marks_account_banned_with_timestamp()
    {
        var account = Account.Create(IdpIssuer.From("local"), IdpSubject.From("user@example.com"));

        var result = account.Ban(Now);

        Assert.True(result.IsSuccess);
        Assert.True(account.IsBanned);
        Assert.False(account.IsEnabled);
        Assert.Equal(Now, account.BannedAt);
    }

    [Fact]
    public void Ban_is_idempotent_failure()
    {
        var account = Account.Create(IdpIssuer.From("local"), IdpSubject.From("user@example.com"));
        Assert.True(account.Ban(Now).IsSuccess);

        var second = account.Ban(Now);

        Assert.False(second.IsSuccess);
        Assert.Equal(IdentityErrors.AccountBanned, second.Error);
    }
}
