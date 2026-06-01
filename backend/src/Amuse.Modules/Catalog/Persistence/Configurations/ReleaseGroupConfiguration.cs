using Amuse.Domain.Catalog;
using Amuse.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amuse.Modules.Catalog.Persistence.Configurations;

internal sealed class ReleaseGroupConfiguration : IEntityTypeConfiguration<ReleaseGroup>
{
    public void Configure(EntityTypeBuilder<ReleaseGroup> builder)
    {
        builder.ToTable("release_group");

        builder.HasKey(g => g.Id);

        builder.Property(g => g.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => ReleaseGroupId.From(value));

        builder.Property(g => g.OrganizationId)
            .HasColumnName("organization_id")
            .HasConversion(id => id.Value, value => OrganizationId.From(value));

        builder.Property(g => g.ArtistId)
            .HasColumnName("artist_id")
            .HasConversion(id => id.Value, value => ArtistId.From(value));

        builder.Property(g => g.Title)
            .HasColumnName("title")
            .HasMaxLength(ReleaseGroup.MaxTitleLength)
            .IsRequired();

        builder.Property(g => g.Slug)
            .HasColumnName("slug")
            .HasMaxLength(Slug.MaxLength)
            .HasConversion(s => s.Value, v => Slug.From(v))
            .IsRequired();

        builder.Property(g => g.Description)
            .HasColumnName("description")
            .HasMaxLength(ReleaseGroup.MaxDescriptionLength);

        builder.Property(g => g.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz");

        builder.Property(g => g.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamptz");

        builder.HasIndex(g => new { g.ArtistId, g.Slug }).IsUnique();
        builder.HasIndex(g => g.OrganizationId);

        builder.HasOne<Artist>()
            .WithMany()
            .HasForeignKey(g => g.ArtistId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
