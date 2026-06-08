using Amuse.Domain.Catalog;
using Amuse.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amuse.Modules.Catalog.Persistence.Configurations;

internal sealed class TrackConfiguration : IEntityTypeConfiguration<Track>
{
    public void Configure(EntityTypeBuilder<Track> builder)
    {
        builder.ToTable("track");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => TrackId.From(value));

        builder.Property(t => t.OrganizationId)
            .HasColumnName("organization_id")
            .HasConversion(id => id.Value, value => OrganizationId.From(value));

        builder.Property(t => t.ReleaseId)
            .HasColumnName("release_id")
            .HasConversion(id => id.Value, value => ReleaseId.From(value));

        builder.Property(t => t.LifecycleStatus)
            .HasColumnName("lifecycle_status")
            .HasColumnType("catalog.track_lifecycle_status");

        builder.Property(t => t.ExplicitFlag)
            .HasColumnName("explicit_flag");

        builder.Property(t => t.Isrc)
            .HasColumnName("isrc")
            .HasMaxLength(Track.MaxIsrcLength);

        builder.Property(t => t.Lyrics)
            .HasColumnName("lyrics")
            .HasMaxLength(Track.MaxLyricsLength);

        builder.Property(t => t.LanguageCode)
            .HasColumnName("language_code")
            .HasMaxLength(Track.MaxLanguageCodeLength);

        builder.Property(t => t.VersionTitle)
            .HasColumnName("version_title")
            .HasMaxLength(Track.MaxVersionTitleLength);

        builder.Property(t => t.ComposerCredits)
            .HasColumnName("composer_credits")
            .HasMaxLength(Track.MaxCreditsLength);

        builder.Property(t => t.Title)
            .HasColumnName("title")
            .HasMaxLength(Track.MaxTitleLength)
            .IsRequired();

        builder.Property(t => t.TrackNumber)
            .HasColumnName("track_number");

        builder.Property(t => t.Duration)
            .HasColumnName("duration_ms")
            .HasConversion(
                d => d.Milliseconds,
                ms => TrackDuration.FromMilliseconds(ms));

        builder.Property(t => t.AudioMasterKey)
            .HasColumnName("audio_master_key")
            .HasMaxLength(Track.MaxKeyLength);

        builder.Property(t => t.AudioStreamKey)
            .HasColumnName("audio_stream_key")
            .HasMaxLength(Track.MaxKeyLength);

        builder.Property(t => t.IsForSale)
            .HasColumnName("is_for_sale");

        builder.Property(t => t.PriceFloorMinor)
            .HasColumnName("price_floor_minor");

        builder.Property(t => t.PriceCeilingMinor)
            .HasColumnName("price_ceiling_minor");

        builder.Property(t => t.PriceCurrency)
            .HasColumnName("price_currency")
            .HasMaxLength(CatalogPricing.CurrencyLength);

        builder.OwnsOne(t => t.LoudnessProfile, loudness =>
        {
            loudness.Property(p => p.IntegratedLufs)
                .HasColumnName("loudness_integrated_lufs");
            loudness.Property(p => p.TruePeakDbtp)
                .HasColumnName("loudness_true_peak_dbtp");
            loudness.Property(p => p.LoudnessRangeLu)
                .HasColumnName("loudness_range_lu");
            loudness.Property(p => p.ThresholdLufs)
                .HasColumnName("loudness_threshold_lufs");
            loudness.Property(p => p.TargetIntegratedLufs)
                .HasColumnName("loudness_target_integrated_lufs");
            loudness.Property(p => p.TargetTruePeakDbtp)
                .HasColumnName("loudness_target_true_peak_dbtp");
            loudness.Property(p => p.LinearGainLu)
                .HasColumnName("loudness_linear_gain_lu");
            loudness.Property(p => p.AnalyzedAt)
                .HasColumnName("loudness_analyzed_at");
        });

        builder.HasIndex(t => new { t.ReleaseId, t.TrackNumber }).IsUnique();
        builder.HasIndex(t => new { t.OrganizationId, t.LifecycleStatus });

        builder
            .Metadata
            .FindNavigation(nameof(Track.Collaborators))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
