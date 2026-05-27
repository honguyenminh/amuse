using Amuse.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amuse.Modules.Catalog.Persistence.Configurations;

internal sealed class TrackConfiguration : IEntityTypeConfiguration<Track>
{
    public void Configure(EntityTypeBuilder<Track> builder)
    {
        builder.ToTable("track");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => TrackId.From(value));

        builder.Property(t => t.ReleaseId)
            .HasColumnName("release_id")
            .HasConversion(id => id.Value, value => ReleaseId.From(value));

        builder.Property(t => t.Title)
            .HasColumnName("title")
            .HasMaxLength(Track.MaxTitleLength)
            .IsRequired();

        builder.Property(t => t.TrackNumber)
            .HasColumnName("track_number");

        builder.Property(t => t.Duration)
            .HasColumnName("duration_ms")
            .HasConversion(
                d => d.Milliseconds,
                ms => TrackDuration.FromMilliseconds(ms));

        builder.Property(t => t.AudioMasterKey)
            .HasColumnName("audio_master_key")
            .HasMaxLength(Track.MaxKeyLength);

        builder.Property(t => t.AudioStreamKey)
            .HasColumnName("audio_stream_key")
            .HasMaxLength(Track.MaxKeyLength);

        builder.HasIndex(t => new { t.ReleaseId, t.TrackNumber }).IsUnique();
    }
}
