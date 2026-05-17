using Amuse.Domain.Listener;
using Amuse.Modules.Common.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Listener.Persistence;

public sealed class ListenerDbContext : ModuleDbContextBase
{
    public ListenerDbContext(DbContextOptions<ListenerDbContext> options)
        : base(options)
    {
    }

    public DbSet<ListenerProfile> ListenerProfiles => Set<ListenerProfile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("listener");
        modelBuilder.ApplyConfigurationsFromNamespace(
            typeof(ListenerDbContext),
            "Amuse.Modules.Listener.Persistence.Configurations");
    }
}
