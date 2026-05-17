using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Amuse.Domain.Identity;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Identity.Options;

namespace Amuse.Modules.Identity.Auth.External;

internal sealed class OAuth2UserInfoExternalIdentityResolver(
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
        if (!payload.TryGetProperty("access_token", out var accessTokenElement))
            return Result<ExternalIdentityProof>.Failure(IdentityErrors.ExternalLoginFailed);

        return await ResolveUserInfoAsync(accessTokenElement.GetString()!, cancellationToken);
    }

    public Task<Result<ExternalIdentityProof>> ResolveIdTokenAsync(string idToken, CancellationToken cancellationToken) =>
        Task.FromResult(Result<ExternalIdentityProof>.Failure(IdentityErrors.ExternalLoginFailed));

    private async Task<Result<ExternalIdentityProof>> ResolveUserInfoAsync(
        string accessToken,
        CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, definition.UserInfoEndpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return Result<ExternalIdentityProof>.Failure(IdentityErrors.ExternalLoginFailed);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        if (!json.TryGetProperty(definition.SubjectClaim, out var subjectElement))
            return Result<ExternalIdentityProof>.Failure(IdentityErrors.ExternalLoginFailed);

        var subject = subjectElement.ValueKind switch
        {
            JsonValueKind.Number => subjectElement.GetRawText(),
            JsonValueKind.String => subjectElement.GetString(),
            _ => null,
        };

        if (string.IsNullOrWhiteSpace(subject))
            return Result<ExternalIdentityProof>.Failure(IdentityErrors.ExternalLoginFailed);

        return Result<ExternalIdentityProof>.Success(new ExternalIdentityProof(
            IdpIssuer.From(definition.Issuer),
            IdpSubject.From(subject)));
    }
}
