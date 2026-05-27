using Amuse.Domain.Catalog;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Catalog.Persistence;
using Amuse.Modules.Media;
using Amuse.Modules.Media.Options;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Amuse.Modules.Catalog.Features.ManageTrackAudio;

public sealed record PresignAudioMasterUploadRequest(
    string FileName,
    string ContentType);

public sealed record PresignAudioMasterUploadResponse(
    Guid TrackId,
    string Key,
    string Url,
    DateTimeOffset ExpiresAt,
    string Method);

internal sealed class PresignAudioMasterUploadRequestValidator : AbstractValidator<PresignAudioMasterUploadRequest>
{
    public PresignAudioMasterUploadRequestValidator()
    {
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(300);
        RuleFor(x => x.ContentType).NotEmpty().MaximumLength(200);
    }
}

internal sealed class PresignAudioMasterUploadHandler(
    CatalogDbContext db,
    IObjectStorage storage,
    IOptions<MediaOptions> mediaOptions)
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "audio/wav",
        "audio/mpeg",
        "audio/mp3",
        "audio/mp4",
        "audio/flac",
        "audio/ogg",
    };

    public async Task<Result<PresignAudioMasterUploadResponse>> HandleAsync(
        Guid trackId,
        PresignAudioMasterUploadRequest request,
        CancellationToken cancellationToken)
    {
        if (trackId == Guid.Empty)
            return Result<PresignAudioMasterUploadResponse>.Failure(CatalogErrors.TrackNotFound);

        if (string.IsNullOrWhiteSpace(request.FileName) || string.IsNullOrWhiteSpace(request.ContentType))
            return Result<PresignAudioMasterUploadResponse>.Failure(CatalogErrors.InvalidAudioUploadRequest);

        if (!AllowedContentTypes.Contains(request.ContentType))
            return Result<PresignAudioMasterUploadResponse>.Failure(CatalogErrors.InvalidAudioUploadRequest);

        var exists = await db.Tracks
            .AsNoTracking()
            .Where(t => t.Id == TrackId.From(trackId))
            .Select(_ => 1)
            .AnyAsync(cancellationToken);

        if (!exists)
            return Result<PresignAudioMasterUploadResponse>.Failure(CatalogErrors.TrackNotFound);

        var ext = ExtensionFromContentType(request.ContentType, request.FileName);
        var key = $"masters/{trackId}/{Guid.CreateVersion7()}{ext}";

        var ttl = TimeSpan.FromMinutes(mediaOptions.Value.SignedUrlMinutes);
        var expiresAt = DateTimeOffset.UtcNow.Add(ttl);
        var url = storage.GetSignedUploadUrl(MediaBucket.Audio, key, ttl, request.ContentType);

        return Result<PresignAudioMasterUploadResponse>.Success(
            new PresignAudioMasterUploadResponse(trackId, key, url, expiresAt, "PUT"));
    }

    private static string ExtensionFromContentType(string contentType, string fileName)
    {
        var ext = Path.GetExtension(fileName);
        if (!string.IsNullOrWhiteSpace(ext) && ext.Length <= 10)
            return ext.ToLowerInvariant();

        return contentType.ToLowerInvariant() switch
        {
            "audio/wav" => ".wav",
            "audio/mpeg" or "audio/mp3" => ".mp3",
            "audio/mp4" => ".m4a",
            "audio/ogg" => ".ogg",
            "audio/flac" => ".flac",
            _ => ".bin",
        };
    }
}

