using Amuse.Modules.Identity.Features.Common;

namespace Amuse.Modules.Identity.Features.RefreshToken;

public sealed record RefreshTokenRequest(
    string? RefreshToken,
    PersonaContextRequest Context);
