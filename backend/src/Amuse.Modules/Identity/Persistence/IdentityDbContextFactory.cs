using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Amuse.Modules.Identity.Persistence;

public sealed class IdentityDbContextFactory : IDesignTimeDbContextFactory<IdentityDbContext>
{
    private const string DefaultConnectionString =
        "Host=localhost;Port=5432;Database=amuse_development;Username=postgres;Password=postgres";

    public IdentityDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? DefaultConnectionString;

        var optionsBuilder = new DbContextOptionsBuilder<IdentityDbContext>();
        IdentityDbContextOptions.Configure(optionsBuilder, connectionString);
        return new IdentityDbContext(optionsBuilder.Options);
    }
}
