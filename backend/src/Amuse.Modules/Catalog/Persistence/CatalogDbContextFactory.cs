using Amuse.Modules.Common.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Amuse.Modules.Catalog.Persistence;

public sealed class CatalogDbContextFactory : IDesignTimeDbContextFactory<CatalogDbContext>
{
    public CatalogDbContext CreateDbContext(string[] args)
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

        var optionsBuilder = new DbContextOptionsBuilder<CatalogDbContext>();
        CatalogDbContextOptions.Configure(optionsBuilder, connectionString);
        return new CatalogDbContext(optionsBuilder.Options, NullOrgScopeAccessor.Instance);
    }
}
