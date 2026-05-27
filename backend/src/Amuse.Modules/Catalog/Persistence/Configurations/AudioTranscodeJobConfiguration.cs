using Amuse.Domain.Catalog;
using Amuse.Modules.Catalog.Processing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amuse.Modules.Catalog.Persistence.Configurations;

internal sealed class AudioTranscodeJobConfiguration : IEntityTypeConfiguration<AudioTranscodeJob>
{
    public void Configure(EntityTypeBuilder<AudioTranscodeJob> builder)
    {
        builder.ToTable("audio_transcode_job");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.TrackId)
            .HasColumnName("track_id")
            .HasConversion(id => id.Value, value => TrackId.From(value));

        builder.Property(x => x.MasterKey)
            .HasColumnName("master_key")
            .HasMaxLength(Track.MaxKeyLength)
            .IsRequired();

        builder.Property(x => x.StreamKey)
            .HasColumnName("stream_key")
            .HasMaxLength(Track.MaxKeyLength)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(x => x.AttemptCount)
            .HasColumnName("attempt_count");

        builder.Property(x => x.LastError)
            .HasColumnName("last_error")
            .HasMaxLength(2000);

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at");

        builder.HasIndex(x => new { x.Status, x.CreatedAt });
        builder.HasIndex(x => x.TrackId);
    }
}

