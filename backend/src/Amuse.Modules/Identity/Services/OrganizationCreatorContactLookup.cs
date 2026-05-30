using System.Text.Json;
using Amuse.Domain.Identity;
using Amuse.Modules.Identity.Persistence;
using Amuse.Modules.Tenancy.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Identity.Services;

/// <summary>
/// Infrastructure adapter: reads account/login contact fields for Tenancy B2B application review.
/// Not part of Identity authentication domain logic.
/// </summary>
internal sealed class OrganizationCreatorContactLookup(IdentityDbContext dbContext)
    : IOrganizationCreatorContactLookup
{
    private static readonly JsonNamingPolicy EnumNaming = JsonNamingPolicy.CamelCase;

    public async Task<IReadOnlyDictionary<Guid, OrganizationApplicationOwner>> GetByAccountIdsAsync(
        IReadOnlyCollection<AccountId> accountIds,
        CancellationToken cancellationToken)
    {
        if (accountIds.Count == 0)
            return new Dictionary<Guid, OrganizationApplicationOwner>();

        var idSet = accountIds.Select(a => a.Value).ToHashSet();

        var accounts = (await dbContext.Accounts
            .AsNoTracking()
            .ToListAsync(cancellationToken))
            .Where(a => idSet.Contains(a.Id.Value))
            .ToList();

        var users = (await dbContext.Users
            .AsNoTracking()
            .ToListAsync(cancellationToken))
            .Where(u =>
                (u.AccountId is not null && idSet.Contains(u.AccountId.Value))
                || idSet.Contains(u.Id))
            .ToList();

        var emailByAccount = new Dictionary<Guid, string>();
        foreach (var user in users)
        {
            var contactEmail = string.IsNullOrWhiteSpace(user.Email)
                ? user.UserName
                : user.Email;
            if (string.IsNullOrWhiteSpace(contactEmail))
                continue;

            var accountId = user.AccountId ?? user.Id;
            if (idSet.Contains(accountId))
                emailByAccount[accountId] = contactEmail;
        }

        return accounts.ToDictionary(
            account => account.Id.Value,
            account => new OrganizationApplicationOwner(
                account.Id.Value,
                emailByAccount.GetValueOrDefault(account.Id.Value),
                account.IdpIssuer.Value,
                account.IdpSubject.Value,
                EnumNaming.ConvertName(account.Status.ToString())));
    }
}
