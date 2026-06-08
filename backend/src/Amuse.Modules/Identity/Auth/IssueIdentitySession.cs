using Amuse.Domain.Identity;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Identity.Contracts;
using Amuse.Modules.Identity.Options;
using Amuse.Modules.Identity.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Identity.Auth;

internal static class IssueIdentitySession
{
    public static async Task<Result<TokenPair>> IssueAsync(
        IdentityDbContext dbContext,
        TokenIssuer tokenIssuer,
        ITenancyPersonaReadModel tenancyReadModel,
        IListenerPersonaReadModel listenerReadModel,
        IPlatformPersonaReadModel platformReadModel,
        JwtOptions jwtOptions,
        Account account,
        PersonaContext personaContext,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (account.IsBanned)
            return Result<TokenPair>.Failure(IdentityErrors.AccountBanned);

        if (!account.IsEnabled)
            return Result<TokenPair>.Failure(IdentityErrors.AccountDisabled);

        var personaResult = await PersonaAccess.ResolveAsync(
            tenancyReadModel,
            listenerReadModel,
            platformReadModel,
            account.Id,
            personaContext,
            cancellationToken);

        if (!personaResult.IsSuccess)
            return Result<TokenPair>.Failure(personaResult.Error!);

        var opaqueRefresh = tokenIssuer.CreateOpaqueRefreshToken();
        var refreshHash = TokenIssuer.HashRefreshToken(opaqueRefresh);
        var refreshExpires = now.AddDays(jwtOptions.RefreshTokenDays);

        var session = RefreshSession.Create(account.Id, refreshHash, refreshExpires, now);
        dbContext.RefreshSessions.Add(session);

        _ = tokenIssuer.CreateRefreshToken(account.Id, session.Id, now);
        var accessJwt = tokenIssuer.CreateAccessToken(account.Id, personaResult.Value!, now);

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<TokenPair>.Success(new TokenPair(
            accessJwt,
            opaqueRefresh,
            now.AddMinutes(jwtOptions.AccessTokenMinutes),
            refreshExpires,
            session.Id.Value));
    }
}
