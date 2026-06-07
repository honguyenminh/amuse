namespace Amuse.Modules.Identity.Features.Common;

public sealed record AuthTokenResponse(
    string AccessToken,
    DateTimeOffset AccessExpiresAt,
    string? RefreshToken,
    DateTimeOffset RefreshExpiresAt);
