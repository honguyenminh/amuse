using Amuse.Domain.Identity;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Common.Time;
using Amuse.Modules.Identity.Auth;
using Amuse.Modules.Identity.Contracts;
using Amuse.Modules.Identity.Features.Shared;
using Amuse.Modules.Identity.Options;
using Amuse.Modules.Identity.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Amuse.Modules.Identity.Features.LoginPassword;

internal sealed class LoginPasswordHandler(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    AccountLinker accountLinker,
    IdentityDbContext dbContext,
    TokenIssuer tokenIssuer,
    ITenancyPersonaReadModel tenancyReadModel,
    IListenerPersonaReadModel listenerReadModel,
    IPlatformPersonaReadModel platformReadModel,
    IOptions<JwtOptions> jwtOptions,
    IClock clock)
{
    public async Task<Result<AuthTokenResponse>> HandleAsync(
        LoginPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var accountResult = await AuthenticateLocalAsync(request.Email, request.Password, cancellationToken);
        if (!accountResult.IsSuccess)
            return Result<AuthTokenResponse>.Failure(accountResult.Error!);

        var personaResult = PersonaContextMapper.ToDomain(request.Context);
        if (!personaResult.IsSuccess)
            return Result<AuthTokenResponse>.Failure(personaResult.Error!);

        var tokens = await IssueIdentitySession.IssueAsync(
            dbContext,
            tokenIssuer,
            tenancyReadModel,
            listenerReadModel,
            platformReadModel,
            jwtOptions.Value,
            accountResult.Value!,
            personaResult.Value!,
            clock.UtcNow,
            cancellationToken);

        if (!tokens.IsSuccess)
            return Result<AuthTokenResponse>.Failure(tokens.Error!);

        return Result<AuthTokenResponse>.Success(new AuthTokenResponse(
            tokens.Value!.AccessToken,
            tokens.Value.AccessExpiresAt,
            tokens.Value.RefreshToken,
            tokens.Value.RefreshExpiresAt));
    }

    private async Task<Result<Account>> AuthenticateLocalAsync(
        string email,
        string password,
        CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
            return Result<Account>.Failure(IdentityErrors.InvalidCredentials);

        var signIn = await signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);
        if (!signIn.Succeeded)
            return Result<Account>.Failure(IdentityErrors.InvalidCredentials);

        if (user.AccountId is null)
        {
            var account = await accountLinker.GetOrCreateAsync(
                IdpIssuer.From(AuthConstants.LocalIdpIssuer),
                IdpSubject.From(user.Id.ToString()),
                cancellationToken);
            user.AccountId = account.Id.Value;
            await userManager.UpdateAsync(user);
            return Result<Account>.Success(account);
        }

        var linked = await accountLinker.GetByIdAsync(AccountId.From(user.AccountId.Value), cancellationToken);
        if (linked is null)
            return Result<Account>.Failure(IdentityErrors.InvalidCredentials);

        return Result<Account>.Success(linked);
    }
}
