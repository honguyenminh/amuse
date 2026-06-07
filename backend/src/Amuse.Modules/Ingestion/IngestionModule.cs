using Amuse.Modules.Common.Persistence;
using Amuse.Modules.Ingestion.Contracts;
using Amuse.Modules.Ingestion.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Amuse.Modules.Ingestion;

public static class IngestionModule
{
    public static IServiceCollection AddIngestionModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddModulePersistenceInfrastructure();
        services.TryAddSingleton(_ => new AuditEntityRegistry());

        services.AddDbContext<IngestionDbContext>((sp, options) =>
        {
            IngestionDbContextOptions.Configure(
                (DbContextOptionsBuilder<IngestionDbContext>)options,
                connectionString);
            options.AddModuleInterceptors(sp);
        });

        services.AddScoped<IIngestionCommands, IngestionCommandsStub>();

        return services;
    }
}
