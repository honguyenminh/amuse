using System.Security.Claims;
using Amuse.Domain.Catalog;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Catalog.Features.Common;
using Amuse.Modules.Catalog.Features.Common;
using Amuse.Modules.Catalog.Persistence;
using Amuse.Modules.Catalog.Processing;
using Amuse.Modules.Common.Time;
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
    IOptions<MediaOptions> mediaOptions,
    IClock clock)
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
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        if (trackId == Guid.Empty)
            return Result<PresignAudioMasterUploadResponse>.Failure(CatalogErrors.TrackNotFound);

        var orgResult = CatalogPersonaAccessor.GetOrganizationId(principal);
        if (!orgResult.IsSuccess)
            return Result<PresignAudioMasterUploadResponse>.Failure(orgResult.Error!);

        if (string.IsNullOrWhiteSpace(request.FileName) || string.IsNullOrWhiteSpace(request.ContentType))
            return Result<PresignAudioMasterUploadResponse>.Failure(CatalogErrors.InvalidAudioUploadRequest);

        if (!AllowedContentTypes.Contains(request.ContentType))
            return Result<PresignAudioMasterUploadResponse>.Failure(CatalogErrors.InvalidAudioUploadRequest);

        var typedId = TrackId.From(trackId);
        var track = await db.Tracks
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == typedId, cancellationToken);

        if (track is null)
            return Result<PresignAudioMasterUploadResponse>.Failure(CatalogErrors.TrackNotFound);

        var scope = CatalogScopeGuard.EnsureOrganizationScope(orgResult.Value!, track.OrganizationId);
        if (!scope.IsSuccess)
            return Result<PresignAudioMasterUploadResponse>.Failure(scope.Error!);

        var ext = ExtensionFromContentType(request.ContentType, request.FileName);
        var key = $"masters/{trackId}/{Guid.CreateVersion7()}{ext}";

        var ttl = TimeSpan.FromMinutes(mediaOptions.Value.SignedUrlMinutes);
        var now = clock.UtcNow;
        var expiresAt = now.Add(ttl);
        var url = storage.GetSignedUploadUrl(MediaBucket.Audio, key, ttl, request.ContentType);

        var intent = AudioMasterUploadIntent.Create(typedId, key, request.ContentType, expiresAt, now);
        db.AudioMasterUploadIntents.Add(intent);
        await db.SaveChangesAsync(cancellationToken);

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
