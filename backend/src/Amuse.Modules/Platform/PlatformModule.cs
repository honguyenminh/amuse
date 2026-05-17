using Amuse.Modules.Identity.Contracts;
using Amuse.Modules.Platform.Options;
using Amuse.Modules.Platform.Persistence;
using Amuse.Modules.Platform.Seeding;
using Amuse.Modules.Platform.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Amuse.Modules.Platform;

public static class PlatformModule
{
    public static IServiceCollection AddPlatformModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.Configure<PlatformRootOptions>(configuration.GetSection(PlatformRootOptions.SectionName));

        services.AddDbContext<PlatformDbContext>((serviceProvider, options) =>
            options
                .UseNpgsql(
                    connectionString,
                    npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory_platform", "platform"))
                .UseSeeding((context, _) =>
                {
                    PlatformRootSeeding
                        .SeedAsync((PlatformDbContext)context, serviceProvider, CancellationToken.None)
                        .GetAwaiter()
                        .GetResult();
                })
                .UseAsyncSeeding((context, _, cancellationToken) =>
                    PlatformRootSeeding.SeedAsync(
                        (PlatformDbContext)context,
                        serviceProvider,
                        cancellationToken)));

        services.AddScoped<IPlatformPersonaReadModel, PlatformPersonaReadModel>();
        return services;
    }
}
