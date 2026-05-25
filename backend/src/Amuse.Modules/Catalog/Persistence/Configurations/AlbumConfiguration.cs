using Amuse.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amuse.Modules.Catalog.Persistence.Configurations;

internal sealed class AlbumConfiguration : IEntityTypeConfiguration<Album>
{
    public void Configure(EntityTypeBuilder<Album> builder)
    {
        builder.ToTable("album");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => AlbumId.From(value));

        builder.Property(a => a.ArtistId)
            .HasColumnName("artist_id")
            .HasConversion(id => id.Value, value => ArtistId.From(value));

        builder.Property(a => a.Title)
            .HasColumnName("title")
            .HasMaxLength(Album.MaxTitleLength)
            .IsRequired();

        builder.Property(a => a.Slug)
            .HasColumnName("slug")
            .HasMaxLength(Slug.MaxLength)
            .HasConversion(s => s.Value, v => Slug.From(v))
            .IsRequired();

        builder.Property(a => a.ReleaseType)
            .HasColumnName("release_type")
            .HasColumnType("catalog.release_type");

        builder.Property(a => a.ReleaseDate)
            .HasColumnName("release_date")
            .HasColumnType("timestamptz");

        builder.Property(a => a.CoverArtUrl)
            .HasColumnName("cover_art_url")
            .HasMaxLength(Album.MaxUrlLength);

        builder.Property(a => a.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz");

        builder.HasIndex(a => new { a.ArtistId, a.Slug }).IsUnique();
        builder.HasIndex(a => a.ReleaseDate);

        builder
            .HasMany(a => a.Tracks)
            .WithOne()
            .HasForeignKey(t => t.AlbumId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .Metadata
            .FindNavigation(nameof(Album.Tracks))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
