using Amuse.Domain.Discovery;
using Amuse.Domain.Listener;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amuse.Modules.Discovery.Persistence.Configurations;

internal sealed class PlaylistFollowConfiguration : IEntityTypeConfiguration<PlaylistFollow>
{
    public void Configure(EntityTypeBuilder<PlaylistFollow> builder)
    {
        builder.ToTable("playlist_follow");

        builder.HasKey(f => new { f.ListenerProfileId, f.PlaylistId });

        builder.Property(f => f.ListenerProfileId)
            .HasColumnName("listener_profile_id")
            .HasConversion(id => id.Value, value => ListenerProfileId.From(value));

        builder.Property(f => f.PlaylistId)
            .HasColumnName("playlist_id")
            .HasConversion(id => id.Value, value => PlaylistId.From(value));

        builder.Property(f => f.FollowedAt)
            .HasColumnName("followed_at")
            .HasColumnType("timestamptz");

        builder.HasIndex(f => f.PlaylistId);
    }
}
