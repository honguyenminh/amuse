using Amuse.Domain.Platform;
using Amuse.Modules.Common.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Platform.Persistence;

public sealed class PlatformDbContext : ModuleDbContextBase
{
    public PlatformDbContext(DbContextOptions<PlatformDbContext> options)
        : base(options)
    {
    }

    public DbSet<PlatformOperator> PlatformOperators => Set<PlatformOperator>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("platform");
        modelBuilder.ApplyConfigurationsFromNamespace(
            typeof(PlatformDbContext),
            "Amuse.Modules.Platform.Persistence.Configurations");
    }
}
