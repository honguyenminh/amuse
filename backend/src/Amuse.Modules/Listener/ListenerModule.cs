using Amuse.Modules.Identity.Contracts;
using Amuse.Modules.Listener.Persistence;
using Amuse.Modules.Listener.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Amuse.Modules.Listener;

public static class ListenerModule
{
    public static IServiceCollection AddListenerModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<ListenerDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory_listener", "listener")));

        services.AddScoped<IListenerPersonaReadModel, ListenerPersonaReadModel>();
        services.AddScoped<EnsureListenerProfileService>();
        return services;
    }
}
