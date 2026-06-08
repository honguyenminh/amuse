using Amuse.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amuse.Modules.Identity.Persistence.Configurations;

internal sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("account");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .HasColumnName("id")
            .HasConversion(
                id => id.Value,
                value => AccountId.From(value));

        builder.Property(a => a.IdpIssuer)
            .HasColumnName("idp_issuer")
            .HasMaxLength(IdpIssuer.MaxLength)
            .HasConversion(
                issuer => issuer.Value,
                value => IdpIssuer.From(value));

        builder.Property(a => a.IdpSubject)
            .HasColumnName("idp_subject")
            .HasMaxLength(IdpSubject.MaxLength)
            .HasConversion(
                subject => subject.Value,
                value => IdpSubject.From(value));

        builder.Property(a => a.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(a => a.BannedAt)
            .HasColumnName("banned_at")
            .HasColumnType("timestamptz");

        builder.HasIndex(a => new { a.IdpIssuer, a.IdpSubject })
            .IsUnique();
    }
}
