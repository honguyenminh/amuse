using System.Security.Claims;
using Amuse.Domain.Catalog;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Catalog.Features.Common;
using Amuse.Modules.Catalog.Features.Shared;
using Amuse.Modules.Catalog.Persistence;
using Amuse.Modules.Common.Time;
using Amuse.Modules.Media;
using Amuse.Modules.Media.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Amuse.Modules.Catalog.Features.ManageArtistCover;

internal sealed class PresignArtistCoverUploadHandler(
    CatalogDbContext db,
    IObjectStorage storage,
    IClock clock,
    IOptions<MediaOptions> mediaOptions)
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp",
    };

    public async Task<Result<PresignArtistCoverUploadResponse>> HandleAsync(
        Guid artistId,
        PresignArtistCoverUploadRequest request,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var orgResult = CatalogPersonaAccessor.GetOrganizationId(principal);
        if (!orgResult.IsSuccess)
            return Result<PresignArtistCoverUploadResponse>.Failure(orgResult.Error!);

        if (artistId == Guid.Empty)
            return Result<PresignArtistCoverUploadResponse>.Failure(CatalogErrors.ArtistNotFound);

        if (string.IsNullOrWhiteSpace(request.FileName) || string.IsNullOrWhiteSpace(request.ContentType))
            return Result<PresignArtistCoverUploadResponse>.Failure(CatalogErrors.InvalidArtistCoverUploadRequest);

        if (!AllowedContentTypes.Contains(request.ContentType))
            return Result<PresignArtistCoverUploadResponse>.Failure(CatalogErrors.InvalidArtistCoverUploadRequest);

        var typedId = ArtistId.From(artistId);
        var artist = await db.Artists
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == typedId, cancellationToken);

        if (artist is null)
            return Result<PresignArtistCoverUploadResponse>.Failure(CatalogErrors.ArtistNotFound);

        var scopeResult = CatalogScopeGuard.EnsureArtistManagedBy(artist, orgResult.Value!);
        if (!scopeResult.IsSuccess)
            return Result<PresignArtistCoverUploadResponse>.Failure(scopeResult.Error!);

        var ext = ExtensionFromContentType(request.ContentType, request.FileName);
        var key = $"artists/{artistId}/{Guid.CreateVersion7()}{ext}";

        var ttl = TimeSpan.FromMinutes(mediaOptions.Value.SignedUrlMinutes);
        var expiresAt = clock.UtcNow.Add(ttl);
        var url = storage.GetSignedUploadUrl(MediaBucket.Covers, key, ttl, request.ContentType);

        return Result<PresignArtistCoverUploadResponse>.Success(
            new PresignArtistCoverUploadResponse(artistId, key, url, expiresAt, "PUT"));
    }

    private static string ExtensionFromContentType(string contentType, string fileName)
    {
        var ext = Path.GetExtension(fileName);
        if (!string.IsNullOrWhiteSpace(ext) && ext.Length <= 10)
            return ext.ToLowerInvariant();

        return contentType.ToLowerInvariant() switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            _ => ".bin",
        };
    }
}

internal sealed class CompleteArtistCoverUploadHandler(
    CatalogDbContext db,
    IObjectStorage storage,
    IMediaPublicUrlBuilder mediaUrls,
    CatalogAuditWriter auditWriter)
{
    public async Task<Result<CompleteArtistCoverUploadResponse>> HandleAsync(
        Guid artistId,
        CompleteArtistCoverUploadRequest request,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var orgResult = CatalogPersonaAccessor.GetOrganizationId(principal);
        if (!orgResult.IsSuccess)
            return Result<CompleteArtistCoverUploadResponse>.Failure(orgResult.Error!);

        if (artistId == Guid.Empty)
            return Result<CompleteArtistCoverUploadResponse>.Failure(CatalogErrors.ArtistNotFound);

        if (string.IsNullOrWhiteSpace(request.Key)
            || !request.Key.StartsWith($"artists/{artistId}/", StringComparison.Ordinal))
        {
            return Result<CompleteArtistCoverUploadResponse>.Failure(CatalogErrors.InvalidArtistCoverUploadRequest);
        }

        var typedId = ArtistId.From(artistId);
        var artist = await db.Artists
            .FirstOrDefaultAsync(a => a.Id == typedId, cancellationToken);

        if (artist is null)
            return Result<CompleteArtistCoverUploadResponse>.Failure(CatalogErrors.ArtistNotFound);

        var scopeResult = CatalogScopeGuard.EnsureArtistManagedBy(artist, orgResult.Value!);
        if (!scopeResult.IsSuccess)
            return Result<CompleteArtistCoverUploadResponse>.Failure(scopeResult.Error!);

        var coverExists = await storage.ObjectExistsAsync(MediaBucket.Covers, request.Key, cancellationToken);
        if (!coverExists)
            return Result<CompleteArtistCoverUploadResponse>.Failure(CatalogErrors.ArtistCoverObjectMissing);

        var before = CatalogAuditSnapshotMapper.FromArtist(artist);
        artist.SetCoverKey(request.Key);
        await db.SaveChangesAsync(cancellationToken);

        await auditWriter.WriteUpdateAsync(
            CatalogAuditTables.Artist,
            artist.Id.Value,
            before,
            CatalogAuditSnapshotMapper.FromArtist(artist),
            CatalogAccountAccessor.TryGetAccountId(principal),
            cancellationToken);

        var coverUrl = mediaUrls.BuildCoverArtUrl(request.Key);
        return Result<CompleteArtistCoverUploadResponse>.Success(
            new CompleteArtistCoverUploadResponse(artistId, request.Key, coverUrl));
    }
}
