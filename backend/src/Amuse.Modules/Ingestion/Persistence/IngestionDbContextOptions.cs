using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Ingestion.Persistence;

internal static class IngestionDbContextOptions
{
    public static void Configure(
        DbContextOptionsBuilder<IngestionDbContext> options,
        string connectionString) =>
        options.UseNpgsql(
            connectionString,
            npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory_ingestion", "ingestion"));
}
