using Amuse.Domain.Identity;
using Amuse.Domain.Listener;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amuse.Modules.Listener.Persistence.Configurations;

internal sealed class ListenerProfileConfiguration : IEntityTypeConfiguration<ListenerProfile>
{
    public void Configure(EntityTypeBuilder<ListenerProfile> builder)
    {
        builder.ToTable("listener_profile");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => ListenerProfileId.From(value));

        builder.Property(p => p.AccountId)
            .HasColumnName("account_id")
            .HasConversion(id => id.Value, value => AccountId.From(value));

        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz");

        builder.HasIndex(p => p.AccountId).IsUnique();
    }
}
