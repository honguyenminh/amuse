using Amuse.Domain.Catalog;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Catalog.Persistence;
using Amuse.Modules.Media;
using Amuse.Modules.Media.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Amuse.Modules.Catalog.Features.GetTrackStreamInfo;

internal sealed class GetTrackStreamInfoHandler(
    CatalogDbContext db,
    IObjectStorage storage,
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
            .Select(t => new { t.Title, t.Duration, t.AudioMasterKey })
            .FirstOrDefaultAsync(cancellationToken);

        if (track is null)
            return Result<TrackStreamInfoResponse>.Failure(CatalogErrors.TrackNotFound);

        if (string.IsNullOrEmpty(track.AudioMasterKey))
            return Result<TrackStreamInfoResponse>.Failure(CatalogErrors.TrackHasNoAudio);

        var ttl = TimeSpan.FromMinutes(mediaOptions.Value.SignedUrlMinutes);
        var url = storage.GetSignedUrl(MediaBucket.Audio, track.AudioMasterKey, ttl);
        var expiresAt = DateTimeOffset.UtcNow.Add(ttl);

        // Content type is inferred from the master key extension. For now seeds use .wav.
        var contentType = ContentTypeForKey(track.AudioMasterKey);

        return Result<TrackStreamInfoResponse>.Success(new TrackStreamInfoResponse(
            trackId,
            url,
            contentType,
            track.Duration.Milliseconds,
            expiresAt));
    }

    private static string ContentTypeForKey(string key)
    {
        var dot = key.LastIndexOf('.');
        if (dot < 0) return "application/octet-stream";
        return key[(dot + 1)..].ToLowerInvariant() switch
        {
            "wav" => "audio/wav",
            "mp3" => "audio/mpeg",
            "m4a" or "mp4" => "audio/mp4",
            "ogg" => "audio/ogg",
            "flac" => "audio/flac",
            _ => "application/octet-stream",
        };
    }
}

public sealed record TrackStreamInfoResponse(
    Guid TrackId,
    string Url,
    string ContentType,
    int DurationMs,
    DateTimeOffset ExpiresAt);
