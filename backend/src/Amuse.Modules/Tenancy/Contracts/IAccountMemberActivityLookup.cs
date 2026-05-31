using Amuse.Domain.Identity;
using Amuse.Domain.Tenancy;

namespace Amuse.Modules.Tenancy.Contracts;

public sealed record AccountMemberActivity(
    string? Email,
    DateTimeOffset? LastLoginAt,
    DateTimeOffset? LastActiveAt,
    DateTimeOffset? JoinedAt);

public interface IAccountMemberActivityLookup
{
    Task<IReadOnlyDictionary<Guid, AccountMemberActivity>> GetByAccountIdsAsync(
        OrganizationId organizationId,
        IReadOnlyCollection<AccountId> accountIds,
        CancellationToken cancellationToken);
}
