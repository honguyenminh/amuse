using Amuse.Domain.Identity;
using Amuse.Domain.Platform;
using Amuse.Modules.Common.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amuse.Modules.Platform.Persistence.Configurations;

internal sealed class PlatformOperatorConfiguration : IEntityTypeConfiguration<PlatformOperator>
{
    public void Configure(EntityTypeBuilder<PlatformOperator> builder)
    {
        builder.ToTable("platform_operator");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd()
            .HasConversion(id => id.Value, value => PlatformOperatorId.From(value));

        builder.Ignore(o => o.IsRoot);

        builder.Property(o => o.AccountId)
            .HasColumnName("account_id")
            .HasConversion(id => id.Value, value => AccountId.From(value));

        builder.Property(o => o.Claims)
            .HasColumnName("claims")
            .HasColumnType("jsonb")
            .HasConversion(JsonStringListConverter.Converter, JsonStringListConverter.Comparer);

        builder.Property(o => o.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz");

        builder.HasIndex(o => o.AccountId).IsUnique();
    }
}
