using Amuse.Modules.Identity.Features.Common;

namespace Amuse.Modules.Identity.Features.LoginPassword;

public sealed record LoginPasswordRequest(
    string Email,
    string Password,
    PersonaContextRequest Context);
