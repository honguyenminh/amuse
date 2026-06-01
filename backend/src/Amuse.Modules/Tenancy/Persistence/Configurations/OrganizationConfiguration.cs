using Amuse.Domain.Identity;
using Amuse.Domain.Platform;
using Amuse.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amuse.Modules.Tenancy.Persistence.Configurations;

internal sealed class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.ToTable("organization");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => OrganizationId.From(value));

        builder.Property(o => o.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(Organization.MaxDisplayNameLength)
            .IsRequired();

        builder.Property(o => o.Description)
            .HasColumnName("description")
            .HasMaxLength(Organization.MaxDescriptionLength);

        builder.Property(o => o.WebsiteUrl)
            .HasColumnName("website_url")
            .HasMaxLength(Organization.MaxWebsiteUrlLength);

        builder.Property(o => o.CountryCode)
            .HasColumnName("country_code")
            .HasMaxLength(Organization.MaxCountryCodeLength);

        builder.Property(o => o.ImprintName)
            .HasColumnName("imprint_name")
            .HasMaxLength(Organization.MaxImprintNameLength);

        builder.Property(o => o.OrgClass)
            .HasColumnName("org_class")
            .HasColumnType("tenancy.org_class");

        builder.Property(o => o.LifecycleStatus)
            .HasColumnName("lifecycle_status")
            .HasColumnType("tenancy.organization_lifecycle_status");

        builder.Property(o => o.OnboardingStatus)
            .HasColumnName("onboarding_status")
            .HasColumnType("tenancy.organization_onboarding_status");

        builder.Property(o => o.TrustTier)
            .HasColumnName("trust_tier")
            .HasColumnType("tenancy.organization_trust_tier");

        builder.Property(o => o.CreatedByAccountId)
            .HasColumnName("created_by_account_id")
            .HasConversion(id => id.Value, value => AccountId.From(value));

        builder.Property(o => o.ApprovedAt)
            .HasColumnName("approved_at")
            .HasColumnType("timestamptz");

        builder.Property(o => o.ApprovedByOperatorId)
            .HasColumnName("approved_by_operator_id")
            .HasConversion(
                id => id.HasValue ? id.Value.Value : (int?)null,
                value => value.HasValue ? PlatformOperatorId.From(value.Value) : null);

        builder.Property(o => o.RejectionReason)
            .HasColumnName("rejection_reason")
            .HasMaxLength(Organization.MaxRejectionReasonLength);

        builder.Property(o => o.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz");

        builder.Property(o => o.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamptz");

        builder.HasIndex(o => o.OnboardingStatus);
        builder.HasIndex(o => o.OrgClass);
    }
}
