using Amuse.Domain.Identity;
using Amuse.Modules.Identity.Persistence;
using Amuse.Modules.Tenancy.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Identity.Services;

internal sealed class AccountEmailLookup(IdentityDbContext dbContext) : IAccountEmailLookup
{
    public async Task<string?> GetEmailAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(
                u => u.AccountId == accountId.Value || u.Id == accountId.Value,
                cancellationToken);

        if (user is null)
            return null;

        return string.IsNullOrWhiteSpace(user.Email) ? user.UserName : user.Email;
    }
}
