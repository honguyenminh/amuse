using Amuse.Domain.Identity;
using Amuse.Domain.SharedKernel;

namespace Amuse.Modules.Identity.Auth.External;

public interface IExternalIdentityResolver
{
    string ProviderName { get; }

    Task<Result<ExternalIdentityProof>> ResolveAuthorizationCodeAsync(
        ExternalAuthorizationCodeRequest request,
        CancellationToken cancellationToken);

    Task<Result<ExternalIdentityProof>> ResolveIdTokenAsync(
        string idToken,
        CancellationToken cancellationToken);
}

public sealed record ExternalAuthorizationCodeRequest(
    string Code,
    string CodeVerifier,
    string RedirectUri,
    string? State);

public sealed record ExternalIdentityProof(IdpIssuer Issuer, IdpSubject Subject);
