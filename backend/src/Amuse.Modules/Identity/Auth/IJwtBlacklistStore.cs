namespace Amuse.Modules.Identity.Auth;

public interface IJwtBlacklistStore
{
    const string RevokedFailureMessage = "identity.token_revoked";

    bool IsRevoked(string jti, DateTimeOffset now);

    Task RevokeAsync(string jti, DateTimeOffset expiresAt, CancellationToken cancellationToken);

    void RememberRevoked(string jti, DateTimeOffset expiresAt);
}
