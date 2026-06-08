using Amuse.Domain.Catalog;
using Amuse.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amuse.Modules.Catalog.Persistence.Configurations;

internal sealed class RoyaltySplitConfiguration : IEntityTypeConfiguration<RoyaltySplit>
{
    public void Configure(EntityTypeBuilder<RoyaltySplit> builder)
    {
        builder.ToTable("royalty_split");

        builder.HasKey(split => split.Id);

        builder.Property(split => split.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => RoyaltySplitId.From(value));

        builder.Property(split => split.TrackId)
            .HasColumnName("track_id")
            .HasConversion(id => id.Value, value => TrackId.From(value));

        builder.Property(split => split.PayeeOrganizationId)
            .HasColumnName("payee_organization_id")
            .HasConversion(id => id.Value, value => OrganizationId.From(value));

        builder.Property(split => split.ShareBps)
            .HasColumnName("share_bps");

        builder.Property(split => split.ListingOrganizationId)
            .HasColumnName("listing_organization_id")
            .HasConversion(id => id.Value, value => OrganizationId.From(value));

        builder.Property(split => split.EffectiveFrom)
            .HasColumnName("effective_from")
            .HasColumnType("timestamptz");

        builder.HasIndex(split => new { split.TrackId, split.PayeeOrganizationId }).IsUnique();
        builder.HasIndex(split => split.ListingOrganizationId);
    }
}
