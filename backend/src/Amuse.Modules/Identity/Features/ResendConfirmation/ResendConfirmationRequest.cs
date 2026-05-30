using Amuse.Modules.Identity.Features.RegisterPassword;

namespace Amuse.Modules.Identity.Features.ResendConfirmation;

public sealed record ResendConfirmationRequest(string Email, RegistrationPortal Portal);
