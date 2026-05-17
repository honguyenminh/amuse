using Amuse.Domain.Identity;
using Amuse.Modules.Common.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Identity.Persistence;

public sealed class IdentityDbContext : ModuleDbContextBase
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options)
        : base(options)
    {
    }

    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<RefreshSession> RefreshSessions => Set<RefreshSession>();
    public DbSet<TokenBlacklistEntry> TokenBlacklistEntries => Set<TokenBlacklistEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("identity");
        modelBuilder.ApplyConfigurationsFromNamespace(
            typeof(IdentityDbContext),
            "Amuse.Modules.Identity.Persistence.Configurations");
    }
}
