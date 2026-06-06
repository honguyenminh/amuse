using Amuse.Domain.Discovery;
using Amuse.Domain.Listener;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amuse.Modules.Discovery.Persistence.Configurations;

internal sealed class PlaylistConfiguration : IEntityTypeConfiguration<Playlist>
{
    public void Configure(EntityTypeBuilder<Playlist> builder)
    {
        builder.ToTable("playlist");

        builder.HasKey(p => p.Id);

        builder.Ignore(p => p.BecamePrivate);

        builder.Property(p => p.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => PlaylistId.From(value));

        builder.Property(p => p.OwnerListenerProfileId)
            .HasColumnName("owner_listener_profile_id")
            .HasConversion(id => id.Value, value => ListenerProfileId.From(value));

        builder.Property(p => p.Title)
            .HasColumnName("title")
            .HasMaxLength(PlaylistTitle.MaxLength)
            .IsRequired();

        builder.Property(p => p.Description)
            .HasColumnName("description")
            .HasMaxLength(Playlist.MaxDescriptionLength);

        builder.Property(p => p.Kind)
            .HasColumnName("kind")
            .HasColumnType("discovery.playlist_kind");

        builder.Property(p => p.Visibility)
            .HasColumnName("visibility")
            .HasColumnType("discovery.playlist_visibility");

        builder.Property(p => p.ForkedFromPlaylistId)
            .HasColumnName("forked_from_playlist_id")
            .HasConversion(
                id => id.HasValue ? id.Value.Value : (Guid?)null,
                value => value.HasValue ? PlaylistId.From(value.Value) : null);

        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz");

        builder.Property(p => p.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamptz");

        builder.HasIndex(p => p.OwnerListenerProfileId);
        builder.HasIndex(p => p.OwnerListenerProfileId)
            .HasDatabaseName("ix_playlist_owner_liked_unique")
            .IsUnique()
            .HasFilter("kind = 'liked'::discovery.playlist_kind");
        builder.HasIndex(p => new { p.Visibility, p.Title });

        builder
            .HasMany(p => p.Items)
            .WithOne()
            .HasForeignKey("playlist_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .Metadata
            .FindNavigation(nameof(Playlist.Items))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder
            .HasMany(p => p.ShareGrants)
            .WithOne()
            .HasForeignKey("playlist_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .Metadata
            .FindNavigation(nameof(Playlist.ShareGrants))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
