using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Amuse.Modules.Ingestion.Persistence;

public sealed class IngestionDbContextFactory : IDesignTimeDbContextFactory<IngestionDbContext>
{
    public IngestionDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../Amuse.Api"))
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=amuse_development;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<IngestionDbContext>();
        IngestionDbContextOptions.Configure(optionsBuilder, connectionString);
        return new IngestionDbContext(optionsBuilder.Options);
    }
}
