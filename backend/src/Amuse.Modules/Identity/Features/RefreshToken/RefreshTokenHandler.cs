using Amuse.Domain.Identity;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Common.Time;
using Amuse.Modules.Identity.Auth;
using Amuse.Modules.Identity.Contracts;
using Amuse.Modules.Identity.Features.Common;
using Amuse.Modules.Identity.Options;
using Amuse.Modules.Identity.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Amuse.Modules.Identity.Features.RefreshToken;

internal sealed class RefreshTokenHandler(
    IdentityDbContext dbContext,
    TokenIssuer tokenIssuer,
    ITenancyPersonaReadModel tenancyReadModel,
    IListenerPersonaReadModel listenerReadModel,
    IPlatformPersonaReadModel platformReadModel,
    IOptions<JwtOptions> jwtOptions,
    IClock clock)
{
    public async Task<Result<AuthTokenResponse>> HandleAsync(
        RefreshTokenRequest request,
        string? refreshToken,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return Result<AuthTokenResponse>.Failure(IdentityErrors.InvalidRefreshToken);

        var now = clock.UtcNow;
        var hash = TokenIssuer.HashRefreshToken(refreshToken);
        var session = await dbContext.RefreshSessions
            .FirstOrDefaultAsync(s => s.TokenHash == hash, cancellationToken);

        if (session is null || !session.IsActive(now))
            return Result<AuthTokenResponse>.Failure(IdentityErrors.InvalidRefreshToken);

        var account = await dbContext.Accounts.FirstOrDefaultAsync(a => a.Id == session.AccountId, cancellationToken);
        if (account is null || !account.IsEnabled)
            return Result<AuthTokenResponse>.Failure(IdentityErrors.AccountDisabled);

        var personaResult = await PersonaContextBootstrap.ResolveAsync(
            request.Context,
            account.Id,
            listenerReadModel,
            cancellationToken);
        if (!personaResult.IsSuccess)
            return Result<AuthTokenResponse>.Failure(personaResult.Error!);

        session.Revoke(now);

        var tokens = await IssueIdentitySession.IssueAsync(
            dbContext,
            tokenIssuer,
            tenancyReadModel,
            listenerReadModel,
            platformReadModel,
            jwtOptions.Value,
            account,
            personaResult.Value!,
            now,
            cancellationToken);

        if (!tokens.IsSuccess)
            return Result<AuthTokenResponse>.Failure(tokens.Error!);

        return Result<AuthTokenResponse>.Success(new AuthTokenResponse(
            tokens.Value!.AccessToken,
            tokens.Value.AccessExpiresAt,
            tokens.Value.RefreshToken,
            tokens.Value.RefreshExpiresAt));
    }
}
