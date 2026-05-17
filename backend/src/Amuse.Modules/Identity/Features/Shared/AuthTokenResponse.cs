namespace Amuse.Modules.Identity.Features.Shared;

public sealed record AuthTokenResponse(
    string AccessToken,
    DateTimeOffset AccessExpiresAt,
    string? RefreshToken,
    DateTimeOffset RefreshExpiresAt);
