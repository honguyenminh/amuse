using Amuse.Domain.Discovery;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amuse.Modules.Discovery.Persistence.Configurations;

internal sealed class PlaylistShareGrantConfiguration : IEntityTypeConfiguration<PlaylistShareGrant>
{
    public void Configure(EntityTypeBuilder<PlaylistShareGrant> builder)
    {
        builder.ToTable("playlist_share_grant");

        builder.HasKey("playlist_id", nameof(PlaylistShareGrant.Email));

        builder.Property<PlaylistId>("playlist_id")
            .HasColumnName("playlist_id")
            .HasConversion(id => id.Value, value => PlaylistId.From(value));

        builder.Property(g => g.Email)
            .HasColumnName("email")
            .HasMaxLength(320)
            .HasConversion(
                email => email.Value,
                value => new ShareGrantEmail(value))
            .IsRequired();

        builder.Property(g => g.GrantedAt)
            .HasColumnName("granted_at")
            .HasColumnType("timestamptz");
    }
}
