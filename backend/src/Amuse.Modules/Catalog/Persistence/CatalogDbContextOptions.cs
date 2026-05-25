using Amuse.Domain.Catalog;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Catalog.Persistence;

internal static class CatalogDbContextOptions
{
    public static void Configure(
        DbContextOptionsBuilder<CatalogDbContext> options,
        string connectionString) =>
        options.UseNpgsql(
            connectionString,
            npgsql =>
            {
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory_catalog", "catalog");
                npgsql.MapEnum<ReleaseType>("release_type", "catalog");
            });
}
