using Amuse.Domain.Identity;
using Amuse.Domain.Tenancy;
using Amuse.Modules.Common.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amuse.Modules.Tenancy.Persistence.Configurations;

internal sealed class OrganizationInviteConfiguration : IEntityTypeConfiguration<OrganizationInvite>
{
    public void Configure(EntityTypeBuilder<OrganizationInvite> builder)
    {
        builder.ToTable("organization_invite");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Id).HasColumnName("id");

        builder.Property(i => i.OrganizationId)
            .HasColumnName("organization_id")
            .HasConversion(id => id.Value, value => OrganizationId.From(value));

        builder.Property(i => i.Email)
            .HasColumnName("email")
            .HasMaxLength(OrganizationInvite.MaxEmailLength);

        builder.Property(i => i.InvitedByAccountId)
            .HasColumnName("invited_by_account_id")
            .HasConversion(id => id.Value, value => AccountId.From(value));

        builder.Property(i => i.PresetRoleLabel)
            .HasColumnName("preset_role_label")
            .HasMaxLength(64);

        builder.Property(i => i.Claims)
            .HasColumnName("claims")
            .HasColumnType("jsonb")
            .HasConversion(JsonStringListConverter.Converter, JsonStringListConverter.Comparer);

        builder.Property(i => i.TokenHash)
            .HasColumnName("token_hash")
            .HasMaxLength(64);

        builder.Property(i => i.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(i => i.ExpiresAt)
            .HasColumnName("expires_at")
            .HasColumnType("timestamptz");

        builder.Property(i => i.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz");

        builder.Property(i => i.AcceptedAt)
            .HasColumnName("accepted_at")
            .HasColumnType("timestamptz");

        builder.Property(i => i.AcceptedByAccountId)
            .HasColumnName("accepted_by_account_id")
            .HasConversion(
                id => id == null ? (Guid?)null : id.Value.Value,
                value => value == null ? null : AccountId.From(value.Value));

        builder.HasIndex(i => i.TokenHash).IsUnique();
        builder.HasIndex(i => new { i.OrganizationId, i.Email, i.Status })
            .HasFilter("status = 'Pending'")
            .IsUnique();

        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(i => i.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
