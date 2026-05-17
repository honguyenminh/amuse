using Amuse.Modules.Identity.Features.Shared;

namespace Amuse.Modules.Identity.Features.ExternalLoginComplete;

public sealed record ExternalLoginCompleteRequest(
    string Provider,
    ExternalLoginGrantType GrantType,
    string? Code,
    string? CodeVerifier,
    string? RedirectUri,
    string? State,
    string? IdToken,
    PersonaContextRequest Context);
