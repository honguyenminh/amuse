using Amuse.Domain.Identity;
using Amuse.Modules.Identity.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Identity.Auth;

internal sealed class AccountLinker(IdentityDbContext dbContext)
{
    public async Task<Account> GetOrCreateAsync(
        IdpIssuer issuer,
        IdpSubject subject,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.Accounts
            .FirstOrDefaultAsync(a => a.IdpIssuer == issuer && a.IdpSubject == subject, cancellationToken);

        if (existing is not null)
            return existing;

        var account = Account.Create(issuer, subject);
        dbContext.Accounts.Add(account);
        await dbContext.SaveChangesAsync(cancellationToken);
        return account;
    }

    public async Task<Account?> GetByIdAsync(AccountId id, CancellationToken cancellationToken) =>
        await dbContext.Accounts.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
}
