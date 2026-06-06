using Amuse.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amuse.Modules.Catalog.Persistence.Configurations;

internal sealed class TrackAudioRenditionConfiguration : IEntityTypeConfiguration<TrackAudioRendition>
{
    public void Configure(EntityTypeBuilder<TrackAudioRendition> builder)
    {
        builder.ToTable("track_audio_rendition");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id).HasColumnName("id");
        builder.Property(r => r.TrackId)
            .HasColumnName("track_id")
            .HasConversion(id => id.Value, value => TrackId.From(value));

        builder.Property(r => r.Codec)
            .HasColumnName("codec")
            .HasColumnType("catalog.audio_codec");

        builder.Property(r => r.BitrateKbps).HasColumnName("bitrate_kbps");
        builder.Property(r => r.SampleRateHz).HasColumnName("sample_rate_hz");
        builder.Property(r => r.Bandwidth).HasColumnName("bandwidth");
        builder.Property(r => r.RepresentationId)
            .HasColumnName("representation_id")
            .HasMaxLength(32);
        builder.Property(r => r.AdaptationSetId)
            .HasColumnName("adaptation_set_id")
            .HasMaxLength(32);
        builder.Property(r => r.ManifestId).HasColumnName("manifest_id");
        builder.Property(r => r.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone");

        builder.HasIndex(r => new { r.TrackId, r.Codec, r.BitrateKbps })
            .IsUnique();

        builder.HasOne<Track>()
            .WithMany()
            .HasForeignKey(r => r.TrackId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
