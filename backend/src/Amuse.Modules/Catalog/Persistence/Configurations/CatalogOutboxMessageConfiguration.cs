using Amuse.Modules.Catalog.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amuse.Modules.Catalog.Persistence.Configurations;

internal sealed class CatalogOutboxMessageConfiguration : IEntityTypeConfiguration<CatalogOutboxMessage>
{
    public void Configure(EntityTypeBuilder<CatalogOutboxMessage> builder)
    {
        builder.ToTable("outbox_message");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.MessageType)
            .HasColumnName("message_type")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.PayloadJson)
            .HasColumnName("payload_json")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(x => x.ProcessedAt)
            .HasColumnName("processed_at");

        builder.Property(x => x.LastError)
            .HasColumnName("last_error")
            .HasMaxLength(2000);

        builder.Property(x => x.AttemptCount)
            .HasColumnName("attempt_count");

        builder.HasIndex(x => new { x.ProcessedAt, x.CreatedAt })
            .HasFilter("processed_at IS NULL");
    }
}
