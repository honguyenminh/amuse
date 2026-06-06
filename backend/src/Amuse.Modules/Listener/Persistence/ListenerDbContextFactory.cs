using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Amuse.Modules.Listener.Persistence;

public sealed class ListenerDbContextFactory : IDesignTimeDbContextFactory<ListenerDbContext>
{
    public ListenerDbContext CreateDbContext(string[] args)
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

        var optionsBuilder = new DbContextOptionsBuilder<ListenerDbContext>();
        optionsBuilder.UseNpgsql(
            connectionString,
            npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory_listener", "listener"));

        return new ListenerDbContext(optionsBuilder.Options);
    }
}
