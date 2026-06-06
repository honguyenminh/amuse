using Amuse.Domain.Discovery;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Discovery.Persistence;

internal static class DiscoveryDbContextOptions
{
    public static void Configure(
        DbContextOptionsBuilder<DiscoveryDbContext> options,
        string connectionString) =>
        options.UseNpgsql(
            connectionString,
            npgsql =>
            {
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory_discovery", "discovery");
                npgsql.MapEnum<PlaylistKind>("playlist_kind", "discovery");
                npgsql.MapEnum<PlaylistVisibility>("playlist_visibility", "discovery");
                npgsql.MapEnum<LibraryEntryKind>("library_entry_kind", "discovery");
            });
}
