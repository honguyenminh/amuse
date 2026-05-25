using Amuse.Domain.Identity;
using Amuse.Modules.Common.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Identity.Persistence;

public sealed class IdentityDbContext
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
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
        modelBuilder.HasDefaultSchema("identity");
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromNamespace(
            typeof(IdentityDbContext),
            "Amuse.Modules.Identity.Persistence.Configurations");
    }
}
