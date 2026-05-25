using Amuse.Domain.Identity;
using Amuse.Domain.Tenancy;
using Amuse.Modules.Common.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amuse.Modules.Tenancy.Persistence.Configurations;

internal sealed class OrganizationMemberConfiguration : IEntityTypeConfiguration<OrganizationMember>
{
    public void Configure(EntityTypeBuilder<OrganizationMember> builder)
    {
        builder.ToTable("organization_member");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
            .HasColumnName("id");

        builder.Property(m => m.OrganizationId)
            .HasColumnName("organization_id")
            .HasConversion(id => id.Value, value => OrganizationId.From(value));

        builder.Property(m => m.AccountId)
            .HasColumnName("account_id")
            .HasConversion(id => id.Value, value => AccountId.From(value));

        builder.Property(m => m.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(m => m.PresetRoleLabel)
            .HasColumnName("preset_role_label")
            .HasMaxLength(64);

        builder.Property(m => m.Claims)
            .HasColumnName("claims")
            .HasColumnType("jsonb")
            .HasConversion(JsonStringListConverter.Converter, JsonStringListConverter.Comparer);

        builder.Property(m => m.IsOwner)
            .HasColumnName("is_owner");

        builder.HasIndex(m => new { m.OrganizationId, m.AccountId }).IsUnique();
        builder.HasIndex(m => m.AccountId);
    }
}
