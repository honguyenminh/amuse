using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Text.Json;
using Amuse.Domain.Identity;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Identity.Options;

namespace Amuse.Modules.Identity.Auth.External;

internal sealed class OidcExternalIdentityResolver(
    string providerName,
    ExternalProviderDefinition definition,
    IHttpClientFactory httpClientFactory) : IExternalIdentityResolver
{
    public string ProviderName => providerName;

    public async Task<Result<ExternalIdentityProof>> ResolveAuthorizationCodeAsync(
        ExternalAuthorizationCodeRequest request,
        CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient();
        using var tokenResponse = await client.PostAsync(
            definition.TokenEndpoint,
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["code"] = request.Code,
                ["redirect_uri"] = request.RedirectUri,
                ["client_id"] = definition.ClientId,
                ["client_secret"] = definition.ClientSecret,
                ["code_verifier"] = request.CodeVerifier,
            }),
            cancellationToken);

        if (!tokenResponse.IsSuccessStatusCode)
            return Result<ExternalIdentityProof>.Failure(IdentityErrors.ExternalLoginFailed);

        var payload = await tokenResponse.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        if (!payload.TryGetProperty("id_token", out var idTokenElement))
            return Result<ExternalIdentityProof>.Failure(IdentityErrors.ExternalLoginFailed);

        return await ResolveIdTokenAsync(idTokenElement.GetString()!, cancellationToken);
    }

    public Task<Result<ExternalIdentityProof>> ResolveIdTokenAsync(string idToken, CancellationToken cancellationToken)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(idToken);
            var subject = token.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
            if (string.IsNullOrWhiteSpace(subject))
                return Task.FromResult(Result<ExternalIdentityProof>.Failure(IdentityErrors.ExternalLoginFailed));

            var issuer = string.IsNullOrWhiteSpace(definition.Issuer)
                ? token.Issuer
                : definition.Issuer;

            return Task.FromResult(Result<ExternalIdentityProof>.Success(new ExternalIdentityProof(
                IdpIssuer.From(issuer),
                IdpSubject.From(subject))));
        }
        catch
        {
            return Task.FromResult(Result<ExternalIdentityProof>.Failure(IdentityErrors.ExternalLoginFailed));
        }
    }
}
