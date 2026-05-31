using Amuse.Domain.Identity;
using Amuse.Domain.Tenancy;
using Amuse.Modules.Identity.Persistence;
using Amuse.Modules.Tenancy.Contracts;
using Amuse.Modules.Tenancy.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Identity.Services;

internal sealed class AccountMemberActivityLookup(
    IdentityDbContext identityDb,
    TenancyDbContext tenancyDb) : IAccountMemberActivityLookup
{
    public async Task<IReadOnlyDictionary<Guid, AccountMemberActivity>> GetByAccountIdsAsync(
        OrganizationId organizationId,
        IReadOnlyCollection<AccountId> accountIds,
        CancellationToken cancellationToken)
    {
        if (accountIds.Count == 0)
            return new Dictionary<Guid, AccountMemberActivity>();

        var accountIdList = accountIds.Select(a => a.Value).Distinct().ToList();
        var idSet = accountIdList.ToHashSet();

        var users = await identityDb.Users
            .AsNoTracking()
            .Where(u =>
                (u.AccountId != null && accountIdList.Contains(u.AccountId.Value))
                || accountIdList.Contains(u.Id))
            .ToListAsync(cancellationToken);

        var emailByAccount = new Dictionary<Guid, string>();
        foreach (var user in users)
        {
            var contact = string.IsNullOrWhiteSpace(user.Email) ? user.UserName : user.Email;
            if (string.IsNullOrWhiteSpace(contact))
                continue;

            var accountId = user.AccountId ?? user.Id;
            if (idSet.Contains(accountId))
                emailByAccount[accountId] = contact;
        }

        var sessions = await identityDb.RefreshSessions
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var activityByAccount = sessions
            .Where(s => idSet.Contains(s.AccountId.Value))
            .GroupBy(s => s.AccountId.Value)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var last = g.Max(x => x.CreatedAt);
                    return (LastLoginAt: last, LastActiveAt: last);
                });

        var acceptedInvites = await tenancyDb.OrganizationInvites
            .AsNoTracking()
            .Where(i =>
                i.OrganizationId == organizationId
                && i.Status == OrganizationInviteStatus.Accepted)
            .ToListAsync(cancellationToken);

        var joinedByAccount = acceptedInvites
            .Where(i => i.AcceptedByAccountId is not null && idSet.Contains(i.AcceptedByAccountId.Value.Value))
            .GroupBy(i => i.AcceptedByAccountId!.Value.Value)
            .ToDictionary(g => g.Key, g => g.Max(i => i.AcceptedAt!.Value));

        var result = new Dictionary<Guid, AccountMemberActivity>(idSet.Count);
        foreach (var accountId in idSet)
        {
            DateTimeOffset? lastLogin = null;
            DateTimeOffset? lastActive = null;
            if (activityByAccount.TryGetValue(accountId, out var sessionTimes))
            {
                lastLogin = sessionTimes.LastLoginAt;
                lastActive = sessionTimes.LastActiveAt;
            }

            joinedByAccount.TryGetValue(accountId, out var joinedAt);
            emailByAccount.TryGetValue(accountId, out var email);

            result[accountId] = new AccountMemberActivity(
                email,
                lastLogin,
                lastActive,
                joinedAt);
        }

        return result;
    }
}
