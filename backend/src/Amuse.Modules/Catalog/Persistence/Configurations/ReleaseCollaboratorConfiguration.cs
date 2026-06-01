using Amuse.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amuse.Modules.Catalog.Persistence.Configurations;

internal sealed class ReleaseCollaboratorConfiguration : IEntityTypeConfiguration<ReleaseCollaborator>
{
    public void Configure(EntityTypeBuilder<ReleaseCollaborator> builder)
    {
        builder.ToTable("release_collaborator");

        builder.HasKey(c => new { c.ReleaseId, c.ArtistId, c.Role });

        builder.Property(c => c.ReleaseId)
            .HasColumnName("release_id")
            .HasConversion(id => id.Value, value => ReleaseId.From(value));

        builder.Property(c => c.ArtistId)
            .HasColumnName("artist_id")
            .HasConversion(id => id.Value, value => ArtistId.From(value));

        builder.Property(c => c.Role)
            .HasColumnName("role")
            .HasColumnType("catalog.release_collaborator_role");

        builder.Property(c => c.DisplayOrder)
            .HasColumnName("display_order");

        builder.HasIndex(c => new { c.ReleaseId, c.DisplayOrder });

        builder
            .HasOne<Release>()
            .WithMany()
            .HasForeignKey(c => c.ReleaseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne<Artist>()
            .WithMany()
            .HasForeignKey(c => c.ArtistId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
