using Amuse.Modules.Common.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Ingestion.Persistence;

/// <summary>
/// Dedicated ingestion schema. Tables such as audio_master_upload_intent and
/// audio_transcode_job still live in catalog until a follow-up migration moves them here.
/// </summary>
public sealed class IngestionDbContext : ModuleDbContextBase
{
    public IngestionDbContext(DbContextOptions<IngestionDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("ingestion");
    }
}
