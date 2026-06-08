using Amuse.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amuse.Modules.Catalog.Persistence.Configurations;

internal sealed class TrackCollaboratorConfiguration : IEntityTypeConfiguration<TrackCollaborator>
{
    public void Configure(EntityTypeBuilder<TrackCollaborator> builder)
    {
        builder.ToTable("track_collaborator");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => TrackCollaboratorId.From(value));

        builder.Property(c => c.TrackId)
            .HasColumnName("track_id")
            .HasConversion(id => id.Value, value => TrackId.From(value));

        builder.Property(c => c.ArtistId)
            .HasColumnName("artist_id")
            .HasConversion(
                id => id.HasValue ? id.Value.Value : (Guid?)null,
                value => value.HasValue ? ArtistId.From(value.Value) : null);

        builder.Property(c => c.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(TrackCollaborator.MaxDisplayNameLength);

        builder.Property(c => c.Role)
            .HasColumnName("role")
            .HasColumnType("catalog.track_collaborator_role");

        builder.Property(c => c.DisplayOrder)
            .HasColumnName("display_order");

        builder.HasIndex(c => new { c.TrackId, c.DisplayOrder });
        builder.HasIndex(c => new { c.TrackId, c.ArtistId })
            .IsUnique()
            .HasFilter("artist_id IS NOT NULL");

        builder
            .HasOne<Track>()
            .WithMany(t => t.Collaborators)
            .HasForeignKey(c => c.TrackId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne<Artist>()
            .WithMany()
            .HasForeignKey(c => c.ArtistId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
