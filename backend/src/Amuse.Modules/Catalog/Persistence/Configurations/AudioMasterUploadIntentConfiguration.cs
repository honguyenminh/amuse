using Amuse.Domain.Catalog;
using Amuse.Modules.Catalog.Processing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amuse.Modules.Catalog.Persistence.Configurations;

internal sealed class AudioMasterUploadIntentConfiguration : IEntityTypeConfiguration<AudioMasterUploadIntent>
{
    public void Configure(EntityTypeBuilder<AudioMasterUploadIntent> builder)
    {
        builder.ToTable("audio_master_upload_intent");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.TrackId)
            .HasColumnName("track_id")
            .HasConversion(id => id.Value, value => TrackId.From(value));

        builder.Property(x => x.ObjectKey)
            .HasColumnName("object_key")
            .HasMaxLength(Track.MaxKeyLength)
            .IsRequired();

        builder.Property(x => x.ContentType)
            .HasColumnName("content_type")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.ExpiresAt)
            .HasColumnName("expires_at");

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(x => x.ConsumedAt)
            .HasColumnName("consumed_at");

        builder.HasIndex(x => new { x.TrackId, x.ConsumedAt });
        builder.HasIndex(x => x.ObjectKey)
            .IsUnique();
    }
}
