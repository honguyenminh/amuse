using Amuse.Domain.Identity;
using Amuse.Modules.Identity.Auth;
using Amuse.Modules.Identity.Persistence;
using Amuse.Modules.Platform.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Amuse.Modules.Platform.Seeding;

internal static class IdentityRootSeeding
{
    public static async Task EnsureRootAccountAsync(
        IServiceProvider serviceProvider,
        PlatformRootOptions rootOptions,
        CancellationToken cancellationToken)
    {
        var accountId = AccountId.From(rootOptions.AccountId);

        await using var scope = serviceProvider.CreateAsyncScope();
        var identityDb = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

        var existing = await identityDb.Accounts
            .FirstOrDefaultAsync(a => a.Id == accountId, cancellationToken);

        if (existing is null)
        {
            identityDb.Accounts.Add(Account.CreateWithId(
                accountId,
                IdpIssuer.From(AuthConstants.LocalIdpIssuer),
                IdpSubject.From(rootOptions.AccountId.ToString())));
            await identityDb.SaveChangesAsync(cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(rootOptions.Email) || string.IsNullOrWhiteSpace(rootOptions.Password))
            return;

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync(rootOptions.Email);
        if (user is not null)
            return;

        user = new ApplicationUser
        {
            Id = rootOptions.AccountId,
            Email = rootOptions.Email,
            UserName = rootOptions.Email,
            EmailConfirmed = true,
            AccountId = rootOptions.AccountId,
        };

        var result = await userManager.CreateAsync(user, rootOptions.Password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                "Failed to seed root ApplicationUser: " +
                string.Join("; ", result.Errors.Select(e => e.Description)));
        }
    }
}
