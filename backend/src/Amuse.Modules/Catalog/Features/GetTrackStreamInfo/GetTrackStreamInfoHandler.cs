using Amuse.Domain.Catalog;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Catalog.Persistence;
using Amuse.Modules.Media.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Amuse.Modules.Catalog.Features.GetTrackStreamInfo;

internal sealed class GetTrackStreamInfoHandler(
    CatalogDbContext db,
    IOptions<MediaOptions> mediaOptions)
{
    public async Task<Result<TrackStreamInfoResponse>> HandleAsync(
        Guid trackId,
        CancellationToken cancellationToken)
    {
        if (trackId == Guid.Empty)
            return Result<TrackStreamInfoResponse>.Failure(CatalogErrors.TrackNotFound);

        var typedId = TrackId.From(trackId);

        var track = await db.Tracks
            .AsNoTracking()
            .Where(t => t.Id == typedId)
            .Select(t => new
            {
                t.Title,
                t.Duration,
                t.AudioMasterKey,
                t.AudioStreamKey,
                t.LoudnessProfile,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (track is null)
            return Result<TrackStreamInfoResponse>.Failure(CatalogErrors.TrackNotFound);

        if (string.IsNullOrEmpty(track.AudioMasterKey))
            return Result<TrackStreamInfoResponse>.Failure(CatalogErrors.TrackHasNoAudio);

        if (string.IsNullOrWhiteSpace(track.AudioStreamKey))
            return Result<TrackStreamInfoResponse>.Failure(CatalogErrors.TrackStreamNotReady);

        var ttl = TimeSpan.FromMinutes(mediaOptions.Value.SignedUrlMinutes);
        var expiresAt = DateTimeOffset.UtcNow.Add(ttl);

        var chosenKey = track.AudioStreamKey;
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

        var renditions = renditionRows.Count > 0
            ? renditionRows.Select(ToDto).ToList()
            : [LegacyRendition()];

        TrackStreamLoudnessDto? loudness = track.LoudnessProfile is null
            ? null
            : new TrackStreamLoudnessDto(
                track.LoudnessProfile.IntegratedLufs,
                track.LoudnessProfile.TruePeakDbtp,
                track.LoudnessProfile.TargetIntegratedLufs,
                track.LoudnessProfile.TargetTruePeakDbtp,
                track.LoudnessProfile.LinearGainLu);

        return Result<TrackStreamInfoResponse>.Success(new TrackStreamInfoResponse(
            trackId,
            url,
            contentType,
            track.Duration.Milliseconds,
            expiresAt,
            loudness,
            renditions));
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
    IReadOnlyList<TrackStreamRenditionDto> Renditions);
