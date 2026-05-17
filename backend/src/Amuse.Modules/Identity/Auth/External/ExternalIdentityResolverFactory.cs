using Amuse.Modules.Identity.Options;
using Microsoft.Extensions.Options;

namespace Amuse.Modules.Identity.Auth.External;

internal sealed class ExternalIdentityResolverFactory(
    IOptions<ExternalProviderOptions> options,
    IHttpClientFactory httpClientFactory)
{
    public IExternalIdentityResolver? GetResolver(string provider)
    {
        if (!options.Value.Providers.TryGetValue(provider, out var definition))
            return null;

        return definition.Mode.Equals("oauth2", StringComparison.OrdinalIgnoreCase)
            ? new OAuth2UserInfoExternalIdentityResolver(provider, definition, httpClientFactory)
            : new OidcExternalIdentityResolver(provider, definition, httpClientFactory);
    }
}
