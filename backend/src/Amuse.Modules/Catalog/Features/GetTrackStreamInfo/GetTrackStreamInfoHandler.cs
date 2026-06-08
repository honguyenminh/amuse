using System.Security.Claims;
using Amuse.Domain.Catalog;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Billing.Contracts;
using Amuse.Modules.Catalog.Persistence;
using Amuse.Modules.Common.Time;
using Amuse.Modules.Discovery.Features.Common;
using Amuse.Modules.Media.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Amuse.Modules.Catalog.Features.GetTrackStreamInfo;

internal sealed class GetTrackStreamInfoHandler(
    CatalogDbContext db,
    IClock clock,
    IOptions<MediaOptions> mediaOptions,
    IEntitlementReadModel entitlements)
{
    internal const int PublicPreviewMaxBitrateKbps = 128;

    public async Task<Result<TrackStreamInfoResponse>> HandleAsync(
        Guid trackId,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        if (trackId == Guid.Empty)
            return Result<TrackStreamInfoResponse>.Failure(CatalogErrors.TrackNotFound);

        var typedId = TrackId.From(trackId);

        var row = await (
            from track in db.Tracks.AsNoTracking()
            join release in db.Releases.AsNoTracking() on track.ReleaseId equals release.Id
            where track.Id == typedId
            select new
            {
                track.Title,
                track.Duration,
                track.AudioMasterKey,
                track.AudioStreamKey,
                track.LoudnessProfile,
                track.LifecycleStatus,
                ReleaseId = release.Id.Value,
                ReleaseLifecycleStatus = release.LifecycleStatus,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (row is null)
            return Result<TrackStreamInfoResponse>.Failure(CatalogErrors.TrackNotFound);

        if (string.IsNullOrEmpty(row.AudioMasterKey))
            return Result<TrackStreamInfoResponse>.Failure(CatalogErrors.TrackHasNoAudio);

        if (string.IsNullOrWhiteSpace(row.AudioStreamKey))
            return Result<TrackStreamInfoResponse>.Failure(CatalogErrors.TrackStreamNotReady);

        var accountId = DiscoveryPrincipal.ResolveAccountId(principal);
        var isOwner = accountId is not null
            && await entitlements.OwnsTrackAsync(
                accountId.Value,
                trackId,
                row.ReleaseId,
                cancellationToken);

        var isPubliclyStreamable = IsPubliclyStreamable(
            row.LifecycleStatus,
            row.ReleaseLifecycleStatus);

        if (!isOwner && !isPubliclyStreamable)
            return Result<TrackStreamInfoResponse>.Failure(CatalogErrors.StreamPlaybackForbidden);

        var ttl = TimeSpan.FromMinutes(mediaOptions.Value.SignedUrlMinutes);
        var expiresAt = clock.UtcNow.Add(ttl);

        var chosenKey = row.AudioStreamKey;
        var contentType = ContentTypeForKey(chosenKey);

        if (!TryParseDashManifestKey(chosenKey, out var keyTrackId, out var manifestId) || keyTrackId != trackId)
            return Result<TrackStreamInfoResponse>.Failure(CatalogErrors.TrackStreamNotReady);

        var url = $"/api/v1/catalog/tracks/{trackId}/dash/{manifestId}/manifest.mpd";

        var manifestGuid = Guid.Parse(manifestId);
        var renditionRows = await db.TrackAudioRenditions
            .AsNoTracking()
            .Where(r => r.TrackId == typedId && r.ManifestId == manifestGuid)
            .OrderBy(r => r.Codec)
            .ThenBy(r => r.BitrateKbps)
            .ToListAsync(cancellationToken);

        var allRenditions = renditionRows.Count > 0
            ? renditionRows.Select(ToDto).ToList()
            : [LegacyRendition()];

        var renditions = isOwner
            ? allRenditions
            : FilterRenditionsForPublicPreview(allRenditions);

        TrackStreamLoudnessDto? loudness = row.LoudnessProfile is null
            ? null
            : new TrackStreamLoudnessDto(
                row.LoudnessProfile.IntegratedLufs,
                row.LoudnessProfile.TruePeakDbtp,
                row.LoudnessProfile.TargetIntegratedLufs,
                row.LoudnessProfile.TargetTruePeakDbtp,
                row.LoudnessProfile.LinearGainLu);

        return Result<TrackStreamInfoResponse>.Success(new TrackStreamInfoResponse(
            trackId,
            url,
            contentType,
            row.Duration.Milliseconds,
            expiresAt,
            loudness,
            renditions,
            isOwner));
    }

    internal static bool IsPubliclyStreamable(
        TrackLifecycleStatus trackStatus,
        ReleaseLifecycleStatus releaseStatus) =>
        trackStatus == TrackLifecycleStatus.Published
        && releaseStatus == ReleaseLifecycleStatus.Published;

    internal static IReadOnlyList<TrackStreamRenditionDto> FilterRenditionsForPublicPreview(
        IReadOnlyList<TrackStreamRenditionDto> renditions)
    {
        var filtered = renditions.Where(IsPublicPreviewRendition).ToList();
        return filtered.Count > 0 ? filtered : [LegacyRendition()];
    }

    internal static bool IsPublicPreviewRendition(TrackStreamRenditionDto rendition)
    {
        if (string.Equals(rendition.Codec, "flac", StringComparison.OrdinalIgnoreCase))
            return false;

        if (rendition.BitrateKbps is null or > PublicPreviewMaxBitrateKbps)
            return false;

        return true;
    }

    internal static bool IsPublicPreviewRendition(AudioCodec codec, int? bitrateKbps)
    {
        if (codec == AudioCodec.Flac)
            return false;

        return bitrateKbps is null or <= PublicPreviewMaxBitrateKbps;
    }

    private static TrackStreamRenditionDto ToDto(TrackAudioRendition row) =>
        new(
            StableRenditionId(row.Codec, row.BitrateKbps),
            row.Codec.ToString().ToLowerInvariant(),
            row.BitrateKbps,
            row.Bandwidth,
            row.SampleRateHz,
            row.AdaptationSetId,
            row.RepresentationId);

    private static TrackStreamRenditionDto LegacyRendition() =>
        new("aac-128", "aac", 128, 128_000, 48_000, "aac", "0");

    internal static string StableRenditionId(AudioCodec codec, int? bitrateKbps) =>
        codec == AudioCodec.Flac
            ? "flac-0"
            : $"{codec.ToString().ToLowerInvariant()}-{bitrateKbps ?? 0}";

    private static string ContentTypeForKey(string key)
    {
        var dot = key.LastIndexOf('.');
        if (dot < 0) return "application/octet-stream";
        return key[(dot + 1)..].ToLowerInvariant() switch
        {
            "wav" => "audio/wav",
            "mp3" => "audio/mpeg",
            "mpd" => "application/dash+xml",
            "m4s" => "video/mp4",
            "m4a" or "mp4" => "audio/mp4",
            "ogg" => "audio/ogg",
            "flac" => "audio/flac",
            _ => "application/octet-stream",
        };
    }

    private static bool TryParseDashManifestKey(string key, out Guid trackId, out string manifestId)
    {
        trackId = Guid.Empty;
        manifestId = string.Empty;

        var parts = key.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 4) return false;
        if (!string.Equals(parts[0], "dash", StringComparison.Ordinal)) return false;
        if (!Guid.TryParse(parts[1], out trackId)) return false;
        manifestId = parts[2];
        return string.Equals(parts[3], "manifest.mpd", StringComparison.OrdinalIgnoreCase);
    }
}

public sealed record TrackStreamLoudnessDto(
    double IntegratedLufs,
    double TruePeakDbtp,
    double TargetIntegratedLufs,
    double TargetTruePeakDbtp,
    double LinearGainLu);

public sealed record TrackStreamRenditionDto(
    string Id,
    string Codec,
    int? BitrateKbps,
    int Bandwidth,
    int SampleRateHz,
    string AdaptationSetId,
    string RepresentationId);

public sealed record TrackStreamInfoResponse(
    Guid TrackId,
    string Url,
    string ContentType,
    int DurationMs,
    DateTimeOffset ExpiresAt,
    TrackStreamLoudnessDto? Loudness,
    IReadOnlyList<TrackStreamRenditionDto> Renditions,
    bool IsOwner);
