using System.Text.Json;
using Amuse.Modules.Common.Time;
using Amuse.Modules.Identity.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Amuse.Modules.Identity.Auth;

internal sealed partial class JwtBlacklistSyncHostedService(
    IConnectionMultiplexer multiplexer,
    RedisJwtBlacklistStore blacklistStore,
    JwtBlacklistLocalCache localCache,
    IClock clock,
    IOptions<RedisOptions> options,
    ILogger<JwtBlacklistSyncHostedService> logger) : IHostedService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly RedisOptions _options = options.Value;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await HydrateLocalCacheAsync(cancellationToken);

        var subscriber = multiplexer.GetSubscriber();
        await subscriber.SubscribeAsync(
            RedisChannel.Literal(_options.RevokedChannel),
            (_, message) =>
            {
                if (message.IsNullOrEmpty)
                    return;

                try
                {
                    var payload = JsonSerializer.Deserialize<JwtBlacklistRevocationMessage>(
                        message.ToString(),
                        JsonOptions);

                    if (payload is null || string.IsNullOrWhiteSpace(payload.Jti))
                        return;

                    var expiresAt = DateTimeOffset.FromUnixTimeSeconds(payload.ExpUnix);
                    localCache.Remember(payload.Jti, expiresAt);
                }
                catch (JsonException ex)
                {
                    LogInvalidPubSubPayload(ex);
                }
            });
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        var subscriber = multiplexer.GetSubscriber();
        return subscriber.UnsubscribeAsync(RedisChannel.Literal(_options.RevokedChannel));
    }

    private async Task HydrateLocalCacheAsync(CancellationToken cancellationToken)
    {
        var endpoints = multiplexer.GetEndPoints();
        if (endpoints.Length == 0)
            return;

        var server = multiplexer.GetServer(endpoints[0]);
        var database = multiplexer.GetDatabase();
        var now = clock.UtcNow;
        var prefix = _options.BlacklistKeyPrefix;
        var hydrated = 0;

        await foreach (var key in server.KeysAsync(
                           pattern: blacklistStore.BlacklistScanPattern)
                       .WithCancellation(cancellationToken))
        {
            var keyString = key.ToString();
            if (!keyString.StartsWith(prefix, StringComparison.Ordinal))
                continue;

            var jti = keyString[prefix.Length..];
            if (string.IsNullOrWhiteSpace(jti))
                continue;

            var ttl = await database.KeyTimeToLiveAsync(key);
            if (!ttl.HasValue || ttl.Value <= TimeSpan.Zero)
                continue;

            localCache.Remember(jti, now.Add(ttl.Value));
            hydrated++;
        }

        LogHydrated(hydrated);
    }
}
