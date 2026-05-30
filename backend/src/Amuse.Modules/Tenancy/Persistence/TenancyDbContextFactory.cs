using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Amuse.Modules.Tenancy.Persistence;

public sealed class TenancyDbContextFactory : IDesignTimeDbContextFactory<TenancyDbContext>
{
    public TenancyDbContext CreateDbContext(string[] args)
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

        var optionsBuilder = new DbContextOptionsBuilder<TenancyDbContext>();
        TenancyDbContextOptions.Configure(optionsBuilder, connectionString);
        return new TenancyDbContext(optionsBuilder.Options);
    }
}
