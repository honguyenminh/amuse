using Amuse.Domain.SharedKernel;

namespace Amuse.Domain.Identity;

public static class IdentityErrors
{
    public static readonly DomainError InvalidCredentials =
        new("identity.invalid_credentials", "Invalid email or password.");

    public static readonly DomainError AccountDisabled =
        new("identity.account_disabled", "Account is disabled.");

    public static readonly DomainError AccountBanned =
        new("identity.account_banned", "Account is banned.");

    public static readonly DomainError AccountNotFound =
        new("identity.account.not_found", "Account was not found.");

    public static readonly DomainError InvalidRefreshToken =
        new("identity.invalid_refresh_token", "Refresh token is invalid or expired.");

    public static readonly DomainError InvalidPersonaContext =
        new("identity.invalid_persona_context", "Persona context is invalid or not authorized.");

    public static readonly DomainError ExternalLoginFailed =
        new("identity.external_login_failed", "External login could not be completed.");

    public static readonly DomainError TokenRevoked =
        new("identity.token_revoked", "Token has been revoked.");

    public static readonly DomainError EmailAlreadyRegistered =
        new("identity.email_already_registered", "An account with this email already exists.");

    public static readonly DomainError EmailNotConfirmed =
        new("identity.email_not_confirmed", "Confirm your email before signing in.");

    public static readonly DomainError InvalidConfirmationToken =
        new("identity.invalid_confirmation_token", "Email confirmation link is invalid or expired.");

    public static readonly DomainError RegistrationFailed =
        new("identity.registration_failed", "Registration could not be completed.");

    public static readonly DomainError ResendConfirmationRateLimited =
        new("identity.resend_confirmation_rate_limited", "Please wait before requesting another confirmation email.");

    public static DomainError RegistrationFailedWithDetails(string details) =>
        new("identity.registration_failed", details);
}
