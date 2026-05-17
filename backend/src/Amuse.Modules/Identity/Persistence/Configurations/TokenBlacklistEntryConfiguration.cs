using Amuse.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amuse.Modules.Identity.Persistence.Configurations;

internal sealed class TokenBlacklistEntryConfiguration : IEntityTypeConfiguration<TokenBlacklistEntry>
{
    public void Configure(EntityTypeBuilder<TokenBlacklistEntry> builder)
    {
        builder.ToTable("token_blacklist");

        builder.HasKey(e => e.Jti);

        builder.Property(e => e.Jti)
            .HasColumnName("jti")
            .HasMaxLength(64)
            .HasConversion(jti => jti.Value, value => TokenJti.From(value));

        builder.Property(e => e.ExpiresAt)
            .HasColumnName("expires_at")
            .HasColumnType("timestamptz");

        builder.Property(e => e.Reason)
            .HasColumnName("reason")
            .HasMaxLength(256);
    }
}
