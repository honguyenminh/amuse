using Amuse.Domain.Identity;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Common.Time;
using Amuse.Modules.Identity.Auth;
using Amuse.Modules.Identity.Auth.External;
using Amuse.Modules.Identity.Contracts;
using Amuse.Modules.Identity.Features.Shared;
using Amuse.Modules.Identity.Options;
using Amuse.Modules.Identity.Persistence;
using Microsoft.Extensions.Options;

namespace Amuse.Modules.Identity.Features.ExternalLoginComplete;

internal sealed class ExternalLoginCompleteHandler(
    ExternalIdentityResolverFactory resolverFactory,
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
        ExternalLoginCompleteRequest request,
        CancellationToken cancellationToken)
    {
        var resolver = resolverFactory.GetResolver(request.Provider);
        if (resolver is null)
            return Result<AuthTokenResponse>.Failure(IdentityErrors.ExternalLoginFailed);

        var proofResult = request.GrantType switch
        {
            ExternalLoginGrantType.AuthorizationCode => await resolver.ResolveAuthorizationCodeAsync(
                new ExternalAuthorizationCodeRequest(
                    request.Code!,
                    request.CodeVerifier!,
                    request.RedirectUri!,
                    request.State),
                cancellationToken),
            ExternalLoginGrantType.IdToken => await resolver.ResolveIdTokenAsync(request.IdToken!, cancellationToken),
            _ => Result<ExternalIdentityProof>.Failure(IdentityErrors.ExternalLoginFailed),
        };

        if (!proofResult.IsSuccess)
            return Result<AuthTokenResponse>.Failure(proofResult.Error!);

        var account = await accountLinker.GetOrCreateAsync(
            proofResult.Value!.Issuer,
            proofResult.Value.Subject,
            cancellationToken);

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
            account,
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
}
