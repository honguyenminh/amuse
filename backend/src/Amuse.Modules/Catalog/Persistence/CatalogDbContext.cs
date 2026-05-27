using Amuse.Domain.Catalog;
using Amuse.Modules.Common.Persistence;
using Amuse.Modules.Catalog.Processing;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Catalog.Persistence;

public sealed class CatalogDbContext : ModuleDbContextBase
{
    public CatalogDbContext(DbContextOptions<CatalogDbContext> options) : base(options)
    {
    }

    public DbSet<Artist> Artists => Set<Artist>();
    public DbSet<Release> Releases => Set<Release>();
    public DbSet<Track> Tracks => Set<Track>();
    public DbSet<AudioTranscodeJob> AudioTranscodeJobs => Set<AudioTranscodeJob>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("catalog");

        modelBuilder.HasPostgresEnum<ReleaseType>(schema: "catalog", name: "release_type");
        modelBuilder.HasPostgresEnum<AudioTranscodeJobStatus>(schema: "catalog", name: "audio_transcode_job_status");

        modelBuilder.ApplyConfigurationsFromNamespace(
            typeof(CatalogDbContext),
            "Amuse.Modules.Catalog.Persistence.Configurations");
    }
}
