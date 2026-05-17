using Amuse.Domain.SharedKernel;

namespace Amuse.Domain.Identity;

public static class IdentityErrors
{
    public static readonly DomainError InvalidCredentials =
        new("identity.invalid_credentials", "Invalid email or password.");

    public static readonly DomainError AccountDisabled =
        new("identity.account_disabled", "Account is disabled.");

    public static readonly DomainError InvalidRefreshToken =
        new("identity.invalid_refresh_token", "Refresh token is invalid or expired.");

    public static readonly DomainError InvalidPersonaContext =
        new("identity.invalid_persona_context", "Persona context is invalid or not authorized.");

    public static readonly DomainError ExternalLoginFailed =
        new("identity.external_login_failed", "External login could not be completed.");

    public static readonly DomainError TokenRevoked =
        new("identity.token_revoked", "Token has been revoked.");
}
