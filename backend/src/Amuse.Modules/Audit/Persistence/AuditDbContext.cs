using Amuse.Modules.Common.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Audit.Persistence;

public sealed class AuditDbContext : ModuleDbContextBase
{
    public AuditDbContext(DbContextOptions<AuditDbContext> options)
        : base(options)
    {
    }

    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("audit");
        modelBuilder.ApplyConfigurationsFromNamespace(
            typeof(AuditDbContext),
            "Amuse.Modules.Audit.Persistence");
    }
}
