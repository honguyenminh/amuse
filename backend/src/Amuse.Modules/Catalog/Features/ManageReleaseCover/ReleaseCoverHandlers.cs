using System.Security.Claims;
using Amuse.Domain.Catalog;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Catalog.Features.BrowseHome;
using Amuse.Modules.Catalog.Features.Shared;
using Amuse.Modules.Catalog.Persistence;
using Amuse.Modules.Common.Time;
using Amuse.Modules.Media;
using Amuse.Modules.Media.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Amuse.Modules.Catalog.Features.ManageReleaseCover;

internal sealed class PresignReleaseCoverUploadHandler(
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

    public async Task<Result<PresignReleaseCoverUploadResponse>> HandleAsync(
        Guid releaseId,
        PresignReleaseCoverUploadRequest request,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var orgResult = CatalogPersonaAccessor.GetOrganizationId(principal);
        if (!orgResult.IsSuccess)
            return Result<PresignReleaseCoverUploadResponse>.Failure(orgResult.Error!);

        if (releaseId == Guid.Empty)
            return Result<PresignReleaseCoverUploadResponse>.Failure(CatalogErrors.ReleaseNotFound);

        if (string.IsNullOrWhiteSpace(request.FileName) || string.IsNullOrWhiteSpace(request.ContentType))
            return Result<PresignReleaseCoverUploadResponse>.Failure(CatalogErrors.InvalidCoverUploadRequest);

        if (!AllowedContentTypes.Contains(request.ContentType))
            return Result<PresignReleaseCoverUploadResponse>.Failure(CatalogErrors.InvalidCoverUploadRequest);

        var typedId = ReleaseId.From(releaseId);
        var release = await db.Releases
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == typedId, cancellationToken);

        if (release is null)
            return Result<PresignReleaseCoverUploadResponse>.Failure(CatalogErrors.ReleaseNotFound);

        var scopeResult = CatalogScopeGuard.EnsureOrganizationScope(orgResult.Value!, release.OrganizationId);
        if (!scopeResult.IsSuccess)
            return Result<PresignReleaseCoverUploadResponse>.Failure(scopeResult.Error!);

        var ext = ExtensionFromContentType(request.ContentType, request.FileName);
        var key = $"releases/{releaseId}/{Guid.CreateVersion7()}{ext}";

        var ttl = TimeSpan.FromMinutes(mediaOptions.Value.SignedUrlMinutes);
        var expiresAt = DateTimeOffset.UtcNow.Add(ttl);
        var url = storage.GetSignedUploadUrl(MediaBucket.Covers, key, ttl, request.ContentType);

        return Result<PresignReleaseCoverUploadResponse>.Success(
            new PresignReleaseCoverUploadResponse(releaseId, key, url, expiresAt, "PUT"));
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

internal sealed class CompleteReleaseCoverUploadHandler(
    CatalogDbContext db,
    IObjectStorage storage,
    IClock clock,
    CatalogAuditWriter auditWriter)
{
    public async Task<Result<CompleteReleaseCoverUploadResponse>> HandleAsync(
        Guid releaseId,
        CompleteReleaseCoverUploadRequest request,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var orgResult = CatalogPersonaAccessor.GetOrganizationId(principal);
        if (!orgResult.IsSuccess)
            return Result<CompleteReleaseCoverUploadResponse>.Failure(orgResult.Error!);

        if (releaseId == Guid.Empty)
            return Result<CompleteReleaseCoverUploadResponse>.Failure(CatalogErrors.ReleaseNotFound);

        if (string.IsNullOrWhiteSpace(request.Key))
            return Result<CompleteReleaseCoverUploadResponse>.Failure(CatalogErrors.InvalidCoverUploadRequest);

        var typedId = ReleaseId.From(releaseId);
        var release = await db.Releases
            .FirstOrDefaultAsync(r => r.Id == typedId, cancellationToken);

        if (release is null)
            return Result<CompleteReleaseCoverUploadResponse>.Failure(CatalogErrors.ReleaseNotFound);

        var scopeResult = CatalogScopeGuard.EnsureOrganizationScope(orgResult.Value!, release.OrganizationId);
        if (!scopeResult.IsSuccess)
            return Result<CompleteReleaseCoverUploadResponse>.Failure(scopeResult.Error!);

        var coverExists = await storage.ObjectExistsAsync(MediaBucket.Covers, request.Key, cancellationToken);
        if (!coverExists)
            return Result<CompleteReleaseCoverUploadResponse>.Failure(CatalogErrors.CoverObjectMissing);

        var before = CatalogAuditSnapshotMapper.FromRelease(release);
        release.SetCoverArtKey(request.Key, clock.UtcNow);
        await db.SaveChangesAsync(cancellationToken);

        await auditWriter.WriteUpdateAsync(
            CatalogAuditTables.Release,
            release.Id.Value,
            before,
            CatalogAuditSnapshotMapper.FromRelease(release),
            CatalogAccountAccessor.TryGetAccountId(principal),
            cancellationToken);

        var coverUrl = BrowseHomeHandler.CoverArtUrlFor(storage, request.Key);
        return Result<CompleteReleaseCoverUploadResponse>.Success(
            new CompleteReleaseCoverUploadResponse(releaseId, request.Key, coverUrl));
    }
}
