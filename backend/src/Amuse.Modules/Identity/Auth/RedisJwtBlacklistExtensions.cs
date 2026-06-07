using Amuse.Modules.Identity.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Amuse.Modules.Identity.Auth;

internal static class RedisJwtBlacklistExtensions
{
    public static IServiceCollection AddRedisJwtBlacklist(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<RedisOptions>(configuration.GetSection(RedisOptions.SectionName));

        var redis = configuration.GetSection(RedisOptions.SectionName).Get<RedisOptions>()
            ?? new RedisOptions();

        if (string.IsNullOrWhiteSpace(redis.ConnectionString))
        {
            throw new InvalidOperationException(
                $"{RedisOptions.SectionName}:ConnectionString must be configured.");
        }

        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var multiplexer = ConnectionMultiplexer.Connect(redis.ConnectionString);
            if (!multiplexer.IsConnected)
            {
                throw new InvalidOperationException(
                    "Redis connection could not be established. JWT blacklist requires Redis.");
            }

            return multiplexer;
        });

        services.AddSingleton<JwtBlacklistLocalCache>();
        services.AddSingleton<RedisJwtBlacklistStore>();
        services.AddSingleton<IJwtBlacklistStore>(sp => sp.GetRequiredService<RedisJwtBlacklistStore>());
        services.AddHostedService<JwtBlacklistSyncHostedService>();

        return services;
    }
}
