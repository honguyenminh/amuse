using Amuse.Modules.Identity.Features.Shared;

namespace Amuse.Modules.Identity.Features.LoginPassword;

public sealed record LoginPasswordRequest(
    string Email,
    string Password,
    PersonaContextRequest Context);
