using Amuse.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amuse.Modules.Catalog.Persistence.Configurations;

internal sealed class ArtistConfiguration : IEntityTypeConfiguration<Artist>
{
    public void Configure(EntityTypeBuilder<Artist> builder)
    {
        builder.ToTable("artist");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => ArtistId.From(value));

        builder.Property(a => a.Name)
            .HasColumnName("name")
            .HasMaxLength(Artist.MaxNameLength)
            .IsRequired();

        builder.Property(a => a.Slug)
            .HasColumnName("slug")
            .HasMaxLength(Slug.MaxLength)
            .HasConversion(s => s.Value, v => Slug.From(v))
            .IsRequired();

        builder.Property(a => a.Bio)
            .HasColumnName("bio")
            .HasMaxLength(Artist.MaxBioLength);

        builder.Property(a => a.AvatarKey)
            .HasColumnName("avatar_key")
            .HasMaxLength(Artist.MaxKeyLength);

        builder.Property(a => a.CoverKey)
            .HasColumnName("cover_key")
            .HasMaxLength(Artist.MaxKeyLength);

        builder.Property(a => a.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz");

        builder.HasIndex(a => a.Slug).IsUnique();
    }
}
