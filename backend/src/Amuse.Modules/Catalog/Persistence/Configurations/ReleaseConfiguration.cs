using Amuse.Domain.Catalog;
using Amuse.Domain.Tenancy;
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

        builder.Property(r => r.OrganizationId)
            .HasColumnName("organization_id")
            .HasConversion(id => id.Value, value => OrganizationId.From(value));

        builder.Property(r => r.ArtistId)
            .HasColumnName("artist_id")
            .HasConversion(id => id.Value, value => ArtistId.From(value));

        builder.Property(r => r.ReleaseGroupId)
            .HasColumnName("release_group_id")
            .HasConversion(
                id => id.HasValue ? id.Value.Value : (Guid?)null,
                value => value.HasValue ? ReleaseGroupId.From(value.Value) : null);

        builder.Property(r => r.LifecycleStatus)
            .HasColumnName("lifecycle_status")
            .HasColumnType("catalog.release_lifecycle_status");

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

        builder.Property(r => r.Description)
            .HasColumnName("description")
            .HasMaxLength(Release.MaxDescriptionLength);

        builder.Property(r => r.Upc)
            .HasColumnName("upc")
            .HasMaxLength(Release.MaxUpcLength);

        builder.Property(r => r.PrimaryGenre)
            .HasColumnName("primary_genre")
            .HasMaxLength(Release.MaxGenreLength);

        builder.Property(r => r.Tags)
            .HasColumnName("tags")
            .HasMaxLength(Release.MaxTagsLength);

        builder.Property(r => r.LanguageCode)
            .HasColumnName("language_code")
            .HasMaxLength(Release.MaxLanguageCodeLength);

        builder.Property(r => r.LabelName)
            .HasColumnName("label_name")
            .HasMaxLength(Release.MaxLabelNameLength);

        builder.Property(r => r.PLine)
            .HasColumnName("p_line")
            .HasMaxLength(Release.MaxRightsLineLength);

        builder.Property(r => r.CLine)
            .HasColumnName("c_line")
            .HasMaxLength(Release.MaxRightsLineLength);

        builder.Property(r => r.OriginalReleaseDate)
            .HasColumnName("original_release_date")
            .HasColumnType("timestamptz");

        builder.Property(r => r.MetadataComplete)
            .HasColumnName("metadata_complete");

        builder.Property(r => r.CoverArtKey)
            .HasColumnName("cover_art_key")
            .HasMaxLength(Release.MaxKeyLength);

        builder.Property(r => r.PublishedAt)
            .HasColumnName("published_at")
            .HasColumnType("timestamptz");

        builder.Property(r => r.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz");

        builder.Property(r => r.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamptz");

        builder.Property(r => r.IsForSale)
            .HasColumnName("is_for_sale");

        builder.Property(r => r.PriceFloorMinor)
            .HasColumnName("price_floor_minor");

        builder.Property(r => r.PriceCeilingMinor)
            .HasColumnName("price_ceiling_minor");

        builder.Property(r => r.PriceCurrency)
            .HasColumnName("price_currency")
            .HasMaxLength(CatalogPricing.CurrencyLength);

        builder.HasIndex(r => new { r.ArtistId, r.Slug }).IsUnique();
        builder.HasIndex(r => new { r.OrganizationId, r.LifecycleStatus, r.ReleaseDate });
        builder.HasIndex(r => r.ReleaseDate);
        builder.HasIndex(r => r.ReleaseGroupId);
        builder.HasIndex(r => r.Upc);

        builder
            .HasOne<ReleaseGroup>()
            .WithMany()
            .HasForeignKey(r => r.ReleaseGroupId)
            .OnDelete(DeleteBehavior.SetNull);

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
