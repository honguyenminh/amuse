namespace Amuse.Modules.Identity.Options;

public sealed class ExternalProviderOptions
{
    public const string SectionName = "ExternalProviders";

    public Dictionary<string, ExternalProviderDefinition> Providers { get; set; } = new();
}

public sealed class ExternalProviderDefinition
{
    public string Mode { get; set; } = "oidc";
    public string Issuer { get; set; } = string.Empty;
    public string Authority { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string TokenEndpoint { get; set; } = string.Empty;
    public string UserInfoEndpoint { get; set; } = string.Empty;
    public string SubjectClaim { get; set; } = "sub";
}
