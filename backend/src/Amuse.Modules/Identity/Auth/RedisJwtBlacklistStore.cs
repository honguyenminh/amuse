using System.Text.Json;
using Amuse.Modules.Common.Time;
using Amuse.Modules.Identity.Options;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Amuse.Modules.Identity.Auth;

internal sealed class RedisJwtBlacklistStore(
    IConnectionMultiplexer multiplexer,
    JwtBlacklistLocalCache localCache,
    IClock clock,
    IOptions<RedisOptions> options) : IJwtBlacklistStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly RedisOptions _options = options.Value;

    public bool IsRevoked(string jti, DateTimeOffset now) =>
        localCache.IsRevoked(jti, now);

    public void RememberRevoked(string jti, DateTimeOffset expiresAt) =>
        localCache.Remember(jti, expiresAt);

    public async Task RevokeAsync(string jti, DateTimeOffset expiresAt, CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        if (expiresAt <= now)
            return;

        if (localCache.IsRevoked(jti, now))
            return;

        var ttlSeconds = (long)Math.Ceiling((expiresAt - now).TotalSeconds);
        if (ttlSeconds < 1)
            ttlSeconds = 1;

        var key = BlacklistKey(jti);
        var database = multiplexer.GetDatabase();
        await database.StringSetAsync(key, "1", TimeSpan.FromSeconds(ttlSeconds));

        localCache.Remember(jti, expiresAt);

        var message = JsonSerializer.Serialize(
            new JwtBlacklistRevocationMessage(jti, expiresAt.ToUnixTimeSeconds()),
            JsonOptions);

        var subscriber = multiplexer.GetSubscriber();
        await subscriber.PublishAsync(
            RedisChannel.Literal(_options.RevokedChannel),
            message);
    }

    internal string BlacklistKey(string jti) => $"{_options.BlacklistKeyPrefix}{jti}";

    internal string BlacklistScanPattern => $"{_options.BlacklistKeyPrefix}*";
}
