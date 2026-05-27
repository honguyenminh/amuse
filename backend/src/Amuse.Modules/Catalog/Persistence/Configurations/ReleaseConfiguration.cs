using Amuse.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amuse.Modules.Catalog.Persistence.Configurations;

internal sealed class ReleaseConfiguration : IEntityTypeConfiguration<Release>
{
    public void Configure(EntityTypeBuilder<Release> builder)
    {
        builder.ToTable("release");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => ReleaseId.From(value));

        builder.Property(r => r.ArtistId)
            .HasColumnName("artist_id")
            .HasConversion(id => id.Value, value => ArtistId.From(value));

        builder.Property(r => r.Title)
            .HasColumnName("title")
            .HasMaxLength(Release.MaxTitleLength)
            .IsRequired();

        builder.Property(r => r.Slug)
            .HasColumnName("slug")
            .HasMaxLength(Slug.MaxLength)
            .HasConversion(s => s.Value, v => Slug.From(v))
            .IsRequired();

        builder.Property(r => r.ReleaseType)
            .HasColumnName("release_type")
            .HasColumnType("catalog.release_type");

        builder.Property(r => r.ReleaseDate)
            .HasColumnName("release_date")
            .HasColumnType("timestamptz");

        builder.Property(r => r.CoverArtKey)
            .HasColumnName("cover_art_key")
            .HasMaxLength(Release.MaxKeyLength);

        builder.Property(r => r.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz");

        builder.HasIndex(r => new { r.ArtistId, r.Slug }).IsUnique();
        builder.HasIndex(r => r.ReleaseDate);

        builder
            .HasMany(r => r.Tracks)
            .WithOne()
            .HasForeignKey(t => t.ReleaseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .Metadata
            .FindNavigation(nameof(Release.Tracks))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
