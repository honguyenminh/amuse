using System.Security.Claims;
using Amuse.Domain.Catalog;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Catalog.Features.BrowseHome;
using Amuse.Modules.Catalog.Features.Shared;
using Amuse.Modules.Catalog.Persistence;
using Amuse.Modules.Media;
using Amuse.Modules.Media.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Amuse.Modules.Catalog.Features.ManageArtistAvatar;

internal sealed class PresignArtistAvatarUploadHandler(
    CatalogDbContext db,
    IObjectStorage storage,
    IOptions<MediaOptions> mediaOptions)
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp",
    };

    public async Task<Result<PresignArtistAvatarUploadResponse>> HandleAsync(
        Guid artistId,
        PresignArtistAvatarUploadRequest request,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var orgResult = CatalogPersonaAccessor.GetOrganizationId(principal);
        if (!orgResult.IsSuccess)
            return Result<PresignArtistAvatarUploadResponse>.Failure(orgResult.Error!);

        if (artistId == Guid.Empty)
            return Result<PresignArtistAvatarUploadResponse>.Failure(CatalogErrors.ArtistNotFound);

        if (string.IsNullOrWhiteSpace(request.FileName) || string.IsNullOrWhiteSpace(request.ContentType))
            return Result<PresignArtistAvatarUploadResponse>.Failure(CatalogErrors.InvalidArtistAvatarUploadRequest);

        if (!AllowedContentTypes.Contains(request.ContentType))
            return Result<PresignArtistAvatarUploadResponse>.Failure(CatalogErrors.InvalidArtistAvatarUploadRequest);

        var typedId = ArtistId.From(artistId);
        var artist = await db.Artists
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == typedId, cancellationToken);

        if (artist is null)
            return Result<PresignArtistAvatarUploadResponse>.Failure(CatalogErrors.ArtistNotFound);

        var scopeResult = CatalogScopeGuard.EnsureArtistManagedBy(artist, orgResult.Value!);
        if (!scopeResult.IsSuccess)
            return Result<PresignArtistAvatarUploadResponse>.Failure(scopeResult.Error!);

        var ext = ExtensionFromContentType(request.ContentType, request.FileName);
        var key = $"artists/{artistId}/{Guid.CreateVersion7()}{ext}";

        var ttl = TimeSpan.FromMinutes(mediaOptions.Value.SignedUrlMinutes);
        var expiresAt = DateTimeOffset.UtcNow.Add(ttl);
        var url = storage.GetSignedUploadUrl(MediaBucket.Covers, key, ttl, request.ContentType);

        return Result<PresignArtistAvatarUploadResponse>.Success(
            new PresignArtistAvatarUploadResponse(artistId, key, url, expiresAt, "PUT"));
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

internal sealed class CompleteArtistAvatarUploadHandler(
    CatalogDbContext db,
    IObjectStorage storage,
    CatalogAuditWriter auditWriter)
{
    public async Task<Result<CompleteArtistAvatarUploadResponse>> HandleAsync(
        Guid artistId,
        CompleteArtistAvatarUploadRequest request,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var orgResult = CatalogPersonaAccessor.GetOrganizationId(principal);
        if (!orgResult.IsSuccess)
            return Result<CompleteArtistAvatarUploadResponse>.Failure(orgResult.Error!);

        if (artistId == Guid.Empty)
            return Result<CompleteArtistAvatarUploadResponse>.Failure(CatalogErrors.ArtistNotFound);

        if (string.IsNullOrWhiteSpace(request.Key)
            || !request.Key.StartsWith($"artists/{artistId}/", StringComparison.Ordinal))
        {
            return Result<CompleteArtistAvatarUploadResponse>.Failure(CatalogErrors.InvalidArtistAvatarUploadRequest);
        }

        var typedId = ArtistId.From(artistId);
        var artist = await db.Artists
            .FirstOrDefaultAsync(a => a.Id == typedId, cancellationToken);

        if (artist is null)
            return Result<CompleteArtistAvatarUploadResponse>.Failure(CatalogErrors.ArtistNotFound);

        var scopeResult = CatalogScopeGuard.EnsureArtistManagedBy(artist, orgResult.Value!);
        if (!scopeResult.IsSuccess)
            return Result<CompleteArtistAvatarUploadResponse>.Failure(scopeResult.Error!);

        var avatarExists = await storage.ObjectExistsAsync(MediaBucket.Covers, request.Key, cancellationToken);
        if (!avatarExists)
            return Result<CompleteArtistAvatarUploadResponse>.Failure(CatalogErrors.ArtistAvatarObjectMissing);

        var before = CatalogAuditSnapshotMapper.FromArtist(artist);
        artist.SetAvatarKey(request.Key);
        await db.SaveChangesAsync(cancellationToken);

        await auditWriter.WriteUpdateAsync(
            CatalogAuditTables.Artist,
            artist.Id.Value,
            before,
            CatalogAuditSnapshotMapper.FromArtist(artist),
            CatalogAccountAccessor.TryGetAccountId(principal),
            cancellationToken);

        var avatarUrl = BrowseHomeHandler.CoverArtUrlFor(storage, request.Key);
        return Result<CompleteArtistAvatarUploadResponse>.Success(
            new CompleteArtistAvatarUploadResponse(artistId, request.Key, avatarUrl));
    }
}
