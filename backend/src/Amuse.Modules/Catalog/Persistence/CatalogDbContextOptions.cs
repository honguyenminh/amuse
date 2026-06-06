using Amuse.Domain.Catalog;
using Amuse.Modules.Catalog.Processing;
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
                npgsql.MapEnum<AudioTranscodeJobStatus>("audio_transcode_job_status", "catalog");
                npgsql.MapEnum<ArtistVisibilityTier>("artist_visibility_tier", "catalog");
                npgsql.MapEnum<ReleaseLifecycleStatus>("release_lifecycle_status", "catalog");
                npgsql.MapEnum<TrackLifecycleStatus>("track_lifecycle_status", "catalog");
                npgsql.MapEnum<ReleaseCollaboratorRole>("release_collaborator_role", "catalog");
                npgsql.MapEnum<AudioCodec>("audio_codec", "catalog");
            });
}
