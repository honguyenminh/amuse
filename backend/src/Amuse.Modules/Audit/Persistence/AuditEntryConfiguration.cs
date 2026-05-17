using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amuse.Modules.Audit.Persistence;

internal sealed class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry>
{
    public void Configure(EntityTypeBuilder<AuditEntry> builder)
    {
        builder.ToTable("audit_log");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.Action).HasColumnName("action").HasMaxLength(32);
        builder.Property(e => e.TableName).HasColumnName("table_name").HasMaxLength(128);
        builder.Property(e => e.TargetId).HasColumnName("target_id");
        builder.Property(e => e.BeforeJson).HasColumnName("before_json").HasColumnType("jsonb");
        builder.Property(e => e.AfterJson).HasColumnName("after_json").HasColumnType("jsonb");
        builder.Property(e => e.ChangedAt).HasColumnName("changed_at").HasColumnType("timestamptz");
        builder.Property(e => e.ActorAccountId).HasColumnName("actor_account_id");
        builder.Property(e => e.Reason).HasColumnName("reason").HasMaxLength(512);
    }
}
