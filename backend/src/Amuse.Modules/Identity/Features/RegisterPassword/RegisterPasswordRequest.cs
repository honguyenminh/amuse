namespace Amuse.Modules.Identity.Features.RegisterPassword;

public sealed record RegisterPasswordRequest(
    string Email,
    string Password,
    RegistrationPortal Portal);
