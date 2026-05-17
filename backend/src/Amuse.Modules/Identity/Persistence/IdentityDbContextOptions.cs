using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Identity.Persistence;

internal static class IdentityDbContextOptions
{
    public static void Configure(
        DbContextOptionsBuilder<IdentityDbContext> options,
        string connectionString) =>
        options.UseNpgsql(
            connectionString,
            npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory_identity", "identity"));
}
