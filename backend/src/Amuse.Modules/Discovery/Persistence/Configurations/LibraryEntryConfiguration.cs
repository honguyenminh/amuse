using Amuse.Domain.Discovery;
using Amuse.Domain.Listener;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amuse.Modules.Discovery.Persistence.Configurations;

internal sealed class LibraryEntryConfiguration : IEntityTypeConfiguration<LibraryEntry>
{
    public void Configure(EntityTypeBuilder<LibraryEntry> builder)
    {
        builder.ToTable("library_entry");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => LibraryEntryId.From(value));

        builder.Property(e => e.ListenerProfileId)
            .HasColumnName("listener_profile_id")
            .HasConversion(id => id.Value, value => ListenerProfileId.From(value));

        builder.Property(e => e.Kind)
            .HasColumnName("kind")
            .HasColumnType("discovery.library_entry_kind");

        builder.Property(e => e.TargetId)
            .HasColumnName("target_id");

        builder.Property(e => e.SavedAt)
            .HasColumnName("saved_at")
            .HasColumnType("timestamptz");

        builder.HasIndex(e => new { e.ListenerProfileId, e.Kind, e.TargetId }).IsUnique();
        builder.HasIndex(e => new { e.ListenerProfileId, e.Kind });
    }
}
