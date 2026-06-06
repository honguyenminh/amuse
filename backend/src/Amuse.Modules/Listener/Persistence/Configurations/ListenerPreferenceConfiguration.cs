using Amuse.Domain.Identity;
using Amuse.Domain.Listener;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amuse.Modules.Listener.Persistence.Configurations;

internal sealed class ListenerPreferenceConfiguration : IEntityTypeConfiguration<ListenerPreference>
{
    public void Configure(EntityTypeBuilder<ListenerPreference> builder)
    {
        builder.ToTable("listener_preference");

        builder.HasKey(p => p.AccountId);

        builder.Property(p => p.AccountId)
            .HasColumnName("account_id")
            .HasConversion(id => id.Value, value => AccountId.From(value));

        builder.Property(p => p.AllowUnverifiedArtists)
            .HasColumnName("allow_unverified_artists");

        builder.Property(p => p.SetDuringOnboarding)
            .HasColumnName("set_during_onboarding");

        builder.Property(p => p.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamptz");
    }
}
