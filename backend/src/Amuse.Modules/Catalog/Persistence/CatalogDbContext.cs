using Amuse.Domain.Catalog;
using Amuse.Modules.Catalog.Messaging;
using Amuse.Modules.Catalog.Processing;
using Amuse.Modules.Common.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Catalog.Persistence;

public sealed class CatalogDbContext : ModuleDbContextBase
{
    public CatalogDbContext(DbContextOptions<CatalogDbContext> options) : base(options)
    {
    }

    public DbSet<Artist> Artists => Set<Artist>();
    public DbSet<ReleaseGroup> ReleaseGroups => Set<ReleaseGroup>();
    public DbSet<Release> Releases => Set<Release>();
    public DbSet<Track> Tracks => Set<Track>();
    public DbSet<ReleaseCollaborator> ReleaseCollaborators => Set<ReleaseCollaborator>();
    public DbSet<AudioTranscodeJob> AudioTranscodeJobs => Set<AudioTranscodeJob>();
    public DbSet<AudioMasterUploadIntent> AudioMasterUploadIntents => Set<AudioMasterUploadIntent>();
    public DbSet<CatalogOutboxMessage> CatalogOutboxMessages => Set<CatalogOutboxMessage>();
    public DbSet<TrackAudioRendition> TrackAudioRenditions => Set<TrackAudioRendition>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("catalog");

        modelBuilder.HasPostgresEnum<ReleaseType>(schema: "catalog", name: "release_type");
        modelBuilder.HasPostgresEnum<AudioTranscodeJobStatus>(schema: "catalog", name: "audio_transcode_job_status");
        modelBuilder.HasPostgresEnum<ArtistVisibilityTier>(schema: "catalog", name: "artist_visibility_tier");
        modelBuilder.HasPostgresEnum<ReleaseLifecycleStatus>(schema: "catalog", name: "release_lifecycle_status");
        modelBuilder.HasPostgresEnum<TrackLifecycleStatus>(schema: "catalog", name: "track_lifecycle_status");
        modelBuilder.HasPostgresEnum<ReleaseCollaboratorRole>(schema: "catalog", name: "release_collaborator_role");
        modelBuilder.HasPostgresEnum<AudioCodec>(schema: "catalog", name: "audio_codec");

        modelBuilder.ApplyConfigurationsFromNamespace(
            typeof(CatalogDbContext),
            "Amuse.Modules.Catalog.Persistence.Configurations");
    }
}
