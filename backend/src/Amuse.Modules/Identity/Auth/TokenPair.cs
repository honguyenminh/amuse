namespace Amuse.Modules.Identity.Auth;

public sealed record TokenPair(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset AccessExpiresAt,
    DateTimeOffset RefreshExpiresAt,
    Guid RefreshSessionId);
