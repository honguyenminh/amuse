using Amuse.Domain.Catalog;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Catalog.Persistence;
using Amuse.Modules.Media;
using Amuse.Modules.Media.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Amuse.Modules.Catalog.Features.GetTrackDashAsset;

public sealed record DashAssetResponse(
    ReadOnlyMemory<byte> Content,
    string ContentType,
    string? RedirectUrl = null);

internal sealed class GetTrackDashAssetHandler(
    CatalogDbContext db,
    IObjectStorage storage,
    IOptions<MediaOptions> mediaOptions)
{
    public async Task<Result<DashAssetResponse>> HandleAsync(
        Guid trackId,
        string manifestId,
        string assetName,
        CancellationToken cancellationToken)
    {
        if (trackId == Guid.Empty)
            return Result<DashAssetResponse>.Failure(CatalogErrors.TrackNotFound);

        if (!IsSafePathComponent(manifestId) || !IsSafePathComponent(assetName))
            return Result<DashAssetResponse>.Failure(CatalogErrors.InvalidAudioUploadRequest);

        var track = await db.Tracks
            .AsNoTracking()
            .Where(t => t.Id == TrackId.From(trackId))
            .Select(t => new { t.AudioMasterKey, t.AudioStreamKey })
            .FirstOrDefaultAsync(cancellationToken);

        if (track is null)
            return Result<DashAssetResponse>.Failure(CatalogErrors.TrackNotFound);

        if (string.IsNullOrWhiteSpace(track.AudioStreamKey))
            return Result<DashAssetResponse>.Failure(CatalogErrors.TrackStreamNotReady);

        if (!TryParseManifestKey(track.AudioStreamKey, out var parsedTrackId, out var parsedManifestId))
            return Result<DashAssetResponse>.Failure(CatalogErrors.TrackStreamNotReady);

        if (parsedTrackId != trackId || !string.Equals(parsedManifestId, manifestId, StringComparison.Ordinal))
            return Result<DashAssetResponse>.Failure(CatalogErrors.TrackStreamNotReady);

        var basePrefix = $"dash/{trackId}/{manifestId}/";
        var requestedKey = $"{basePrefix}{assetName}";

        if (string.Equals(assetName, "manifest.mpd", StringComparison.OrdinalIgnoreCase))
        {
            var obj = await storage.GetAsync(MediaBucket.Audio, requestedKey, cancellationToken);
            if (obj is null)
                return Result<DashAssetResponse>.Failure(CatalogErrors.StreamAssetNotFound);
            return Result<DashAssetResponse>.Success(
                new DashAssetResponse(obj.Data, "application/dash+xml"));
        }

        var exists = await storage.ObjectExistsAsync(MediaBucket.Audio, requestedKey, cancellationToken);
        if (!exists)
            return Result<DashAssetResponse>.Failure(CatalogErrors.StreamAssetNotFound);

        var ttl = TimeSpan.FromMinutes(mediaOptions.Value.SignedUrlMinutes);
        var signedUrl = storage.GetSignedUrl(MediaBucket.Audio, requestedKey, ttl);
        return Result<DashAssetResponse>.Success(
            new DashAssetResponse(ReadOnlyMemory<byte>.Empty, "application/octet-stream", signedUrl));
    }

    private static bool IsSafePathComponent(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        if (value.Contains('/') || value.Contains('\\')) return false;
        if (value.Contains("..", StringComparison.Ordinal)) return false;
        return value.Length <= 300;
    }

    private static bool TryParseManifestKey(string key, out Guid trackId, out string manifestId)
    {
        trackId = Guid.Empty;
        manifestId = string.Empty;

        var parts = key.Split('/', StringSplitOptions.RemoveEmptyEntries);
        // dash/{trackId}/{manifestId}/manifest.mpd
        if (parts.Length != 4) return false;
        if (!string.Equals(parts[0], "dash", StringComparison.Ordinal)) return false;
        if (!Guid.TryParse(parts[1], out trackId)) return false;
        manifestId = parts[2];
        return string.Equals(parts[3], "manifest.mpd", StringComparison.OrdinalIgnoreCase);
    }
}

