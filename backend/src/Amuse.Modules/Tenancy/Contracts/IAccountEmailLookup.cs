using Amuse.Domain.Identity;

namespace Amuse.Modules.Tenancy.Contracts;

public interface IAccountEmailLookup
{
    Task<string?> GetEmailAsync(AccountId accountId, CancellationToken cancellationToken);
}
