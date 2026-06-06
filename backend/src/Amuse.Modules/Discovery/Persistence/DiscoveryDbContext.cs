using Amuse.Domain.Discovery;
using Amuse.Modules.Common.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Discovery.Persistence;

public sealed class DiscoveryDbContext : ModuleDbContextBase
{
    public DiscoveryDbContext(DbContextOptions<DiscoveryDbContext> options)
        : base(options)
    {
    }

    public DbSet<Playlist> Playlists => Set<Playlist>();
    public DbSet<PlaylistItem> PlaylistItems => Set<PlaylistItem>();
    public DbSet<PlaylistShareGrant> PlaylistShareGrants => Set<PlaylistShareGrant>();
    public DbSet<PlaylistFollow> PlaylistFollows => Set<PlaylistFollow>();
    public DbSet<LibraryEntry> LibraryEntries => Set<LibraryEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("discovery");
        modelBuilder.HasPostgresEnum<PlaylistKind>(schema: "discovery", name: "playlist_kind");
        modelBuilder.HasPostgresEnum<PlaylistVisibility>(schema: "discovery", name: "playlist_visibility");
        modelBuilder.HasPostgresEnum<LibraryEntryKind>(schema: "discovery", name: "library_entry_kind");
        modelBuilder.ApplyConfigurationsFromNamespace(
            typeof(DiscoveryDbContext),
            "Amuse.Modules.Discovery.Persistence.Configurations");
    }
}
