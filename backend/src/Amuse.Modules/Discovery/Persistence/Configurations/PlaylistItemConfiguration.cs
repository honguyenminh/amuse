using Amuse.Domain.Catalog;
using Amuse.Domain.Discovery;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amuse.Modules.Discovery.Persistence.Configurations;

internal sealed class PlaylistItemConfiguration : IEntityTypeConfiguration<PlaylistItem>
{
    public void Configure(EntityTypeBuilder<PlaylistItem> builder)
    {
        builder.ToTable("playlist_item");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => PlaylistItemId.From(value));

        builder.Property<PlaylistId>("playlist_id")
            .HasColumnName("playlist_id")
            .HasConversion(id => id.Value, value => PlaylistId.From(value));

        builder.Property(i => i.TrackId)
            .HasColumnName("track_id")
            .HasConversion(id => id.Value, value => TrackId.From(value));

        builder.Property(i => i.Position)
            .HasColumnName("position");

        builder.Property(i => i.AddedAt)
            .HasColumnName("added_at")
            .HasColumnType("timestamptz");

        builder.HasIndex("playlist_id", nameof(PlaylistItem.Position)).IsUnique();
        builder.HasIndex("playlist_id", nameof(PlaylistItem.TrackId)).IsUnique();
    }
}
