using Amuse.Domain.Catalog;
using Amuse.Domain.Tenancy;
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

        builder.Property(a => a.CountryCode)
            .HasColumnName("country_code")
            .HasMaxLength(Artist.MaxCountryCodeLength);

        builder.Property(a => a.WebsiteUrl)
            .HasColumnName("website_url")
            .HasMaxLength(Artist.MaxUrlLength);

        builder.Property(a => a.Aliases)
            .HasColumnName("aliases")
            .HasMaxLength(Artist.MaxAliasesLength);

        builder.Property(a => a.AvatarKey)
            .HasColumnName("avatar_key")
            .HasMaxLength(Artist.MaxKeyLength);

        builder.Property(a => a.CoverKey)
            .HasColumnName("cover_key")
            .HasMaxLength(Artist.MaxKeyLength);

        builder.Property(a => a.ManagingOrganizationId)
            .HasColumnName("managing_organization_id")
            .HasConversion(
                id => id.HasValue ? id.Value.Value : (Guid?)null,
                value => value.HasValue ? OrganizationId.From(value.Value) : null);

        builder.Property(a => a.VisibilityTier)
            .HasColumnName("visibility_tier")
            .HasColumnType("catalog.artist_visibility_tier");

        builder.Property(a => a.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz");

        builder.HasIndex(a => a.Slug).IsUnique();
        builder.HasIndex(a => a.ManagingOrganizationId);
    }
}
