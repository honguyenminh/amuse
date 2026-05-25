using Amuse.Modules.Identity.Contracts;
using Amuse.Modules.Platform.Options;
using Amuse.Modules.Platform.Persistence;
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

        services.AddDbContext<PlatformDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory_platform", "platform")));

        services.AddScoped<IPlatformPersonaReadModel, PlatformPersonaReadModel>();
        return services;
    }
}
