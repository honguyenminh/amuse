using Amuse.Domain.Catalog;
using Amuse.Modules.Catalog.Messaging;
using Amuse.Modules.Catalog.Processing;
using Amuse.Modules.Common.Authorization;
using Amuse.Modules.Common.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Catalog.Persistence;

public sealed class CatalogDbContext : ModuleDbContextBase
{
    private readonly IOrgScopeAccessor _orgScopeAccessor;

    public CatalogDbContext(
        DbContextOptions<CatalogDbContext> options,
        IOrgScopeAccessor orgScopeAccessor)
        : base(options)
    {
        _orgScopeAccessor = orgScopeAccessor;
    }

    public DbSet<Artist> Artists => Set<Artist>();
    public DbSet<ReleaseGroup> ReleaseGroups => Set<ReleaseGroup>();
    public DbSet<Release> Releases => Set<Release>();
    public DbSet<Track> Tracks => Set<Track>();
    public DbSet<TrackCollaborator> TrackCollaborators => Set<TrackCollaborator>();
    public DbSet<AudioTranscodeJob> AudioTranscodeJobs => Set<AudioTranscodeJob>();
    public DbSet<AudioMasterUploadIntent> AudioMasterUploadIntents => Set<AudioMasterUploadIntent>();
    public DbSet<CatalogOutboxMessage> CatalogOutboxMessages => Set<CatalogOutboxMessage>();
    public DbSet<TrackAudioRendition> TrackAudioRenditions => Set<TrackAudioRendition>();
    public DbSet<RoyaltySplit> RoyaltySplits => Set<RoyaltySplit>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("catalog");

        modelBuilder.HasPostgresEnum<ReleaseType>(schema: "catalog", name: "release_type");
        modelBuilder.HasPostgresEnum<AudioTranscodeJobStatus>(schema: "catalog", name: "audio_transcode_job_status");
        modelBuilder.HasPostgresEnum<ArtistVisibilityTier>(schema: "catalog", name: "artist_visibility_tier");
        modelBuilder.HasPostgresEnum<ReleaseLifecycleStatus>(schema: "catalog", name: "release_lifecycle_status");
        modelBuilder.HasPostgresEnum<TrackLifecycleStatus>(schema: "catalog", name: "track_lifecycle_status");
        modelBuilder.HasPostgresEnum<TrackCollaboratorRole>(schema: "catalog", name: "track_collaborator_role");
        modelBuilder.HasPostgresEnum<AudioCodec>(schema: "catalog", name: "audio_codec");

        modelBuilder.ApplyConfigurationsFromNamespace(
            typeof(CatalogDbContext),
            "Amuse.Modules.Catalog.Persistence.Configurations");

        ApplyTenantQueryFilters(modelBuilder);
    }

    private void ApplyTenantQueryFilters(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Release>()
            .HasQueryFilter(release =>
                _orgScopeAccessor.CurrentOrganizationId == null
                || release.OrganizationId == _orgScopeAccessor.CurrentOrganizationId);

        modelBuilder.Entity<ReleaseGroup>()
            .HasQueryFilter(group =>
                _orgScopeAccessor.CurrentOrganizationId == null
                || group.OrganizationId == _orgScopeAccessor.CurrentOrganizationId);

        modelBuilder.Entity<Track>()
            .HasQueryFilter(track =>
                _orgScopeAccessor.CurrentOrganizationId == null
                || track.OrganizationId == _orgScopeAccessor.CurrentOrganizationId);

        modelBuilder.Entity<Artist>()
            .HasQueryFilter(artist =>
                _orgScopeAccessor.CurrentOrganizationId == null
                || artist.ManagingOrganizationId == _orgScopeAccessor.CurrentOrganizationId);
    }
}
