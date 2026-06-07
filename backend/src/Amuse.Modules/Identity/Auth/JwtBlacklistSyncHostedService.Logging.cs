using Microsoft.Extensions.Logging;

namespace Amuse.Modules.Identity.Auth;

internal sealed partial class JwtBlacklistSyncHostedService
{
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Ignored invalid JWT blacklist pub/sub payload.")]
    private partial void LogInvalidPubSubPayload(Exception ex);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Hydrated {Count} revoked JWT entries into local blacklist cache.")]
    private partial void LogHydrated(int count);
}
