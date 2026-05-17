using Amuse.Modules.Identity.Features.Shared;

namespace Amuse.Modules.Identity.Features.RefreshToken;

public sealed record RefreshTokenRequest(
    string? RefreshToken,
    PersonaContextRequest Context);
