using Amuse.Domain.Identity;
using Amuse.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amuse.Modules.Tenancy.Persistence.Configurations;

internal sealed class BusinessPortalProfileConfiguration : IEntityTypeConfiguration<BusinessPortalProfile>
{
    public void Configure(EntityTypeBuilder<BusinessPortalProfile> builder)
    {
        builder.ToTable("business_portal_profile");

        builder.HasKey(p => p.AccountId);

        builder.Property(p => p.AccountId)
            .HasColumnName("account_id")
            .HasConversion(id => id.Value, value => AccountId.From(value));

        builder.Property(p => p.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(BusinessPortalProfile.MaxDisplayNameLength);

        builder.Property(p => p.AvatarAccentSeed)
            .HasColumnName("avatar_accent_seed");

        builder.Property(p => p.AvatarObjectKey)
            .HasColumnName("avatar_object_key")
            .HasMaxLength(BusinessPortalProfile.MaxAvatarObjectKeyLength);

        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz");

        builder.Property(p => p.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamptz");
    }
}
