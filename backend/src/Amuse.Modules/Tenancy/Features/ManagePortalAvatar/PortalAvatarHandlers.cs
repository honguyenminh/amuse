using System.Security.Claims;
using Amuse.Domain.Identity;
using Amuse.Domain.SharedKernel;
using Amuse.Domain.Tenancy;
using Amuse.Modules.Common.Time;
using Amuse.Modules.Media;
using Amuse.Modules.Media.Options;
using Amuse.Modules.Tenancy.Features.Common;
using Amuse.Modules.Tenancy.Features.GetPortalProfile;
using Amuse.Modules.Tenancy.Services;
using Microsoft.Extensions.Options;

namespace Amuse.Modules.Tenancy.Features.ManagePortalAvatar;

internal sealed class PresignPortalAvatarUploadHandler(
    BusinessPortalProfileService profileService,
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

    public async Task<Result<PresignPortalAvatarUploadResponse>> HandleAsync(
        PresignPortalAvatarUploadRequest request,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var accountResult = TenancyAccountAccessor.GetAccountId(principal);
        if (!accountResult.IsSuccess)
            return Result<PresignPortalAvatarUploadResponse>.Failure(accountResult.Error!);

        if (string.IsNullOrWhiteSpace(request.FileName) || string.IsNullOrWhiteSpace(request.ContentType))
            return Result<PresignPortalAvatarUploadResponse>.Failure(TenancyErrors.InvalidPortalAvatarUploadRequest);

        if (!AllowedContentTypes.Contains(request.ContentType))
            return Result<PresignPortalAvatarUploadResponse>.Failure(TenancyErrors.InvalidPortalAvatarUploadRequest);

        await profileService.GetOrCreateAsync(accountResult.Value!, cancellationToken);

        var ext = ExtensionFromContentType(request.ContentType, request.FileName);
        var key = $"{PortalAvatarStorage.BusinessPrefix(accountResult.Value!)}{Guid.CreateVersion7()}{ext}";
        var ttl = TimeSpan.FromMinutes(mediaOptions.Value.SignedUrlMinutes);
        var expiresAt = clock.UtcNow.Add(ttl);
        var url = storage.GetSignedUploadUrl(MediaBucket.Covers, key, ttl, request.ContentType);

        return Result<PresignPortalAvatarUploadResponse>.Success(
            new PresignPortalAvatarUploadResponse(key, url, expiresAt, "PUT"));
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

internal sealed class CompletePortalAvatarUploadHandler(
    BusinessPortalProfileService profileService,
    IObjectStorage storage,
    IMediaPublicUrlBuilder mediaUrls,
    IClock clock)
{
    public async Task<Result<CompletePortalAvatarUploadResponse>> HandleAsync(
        CompletePortalAvatarUploadRequest request,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var accountResult = TenancyAccountAccessor.GetAccountId(principal);
        if (!accountResult.IsSuccess)
            return Result<CompletePortalAvatarUploadResponse>.Failure(accountResult.Error!);

        if (string.IsNullOrWhiteSpace(request.Key)
            || !PortalAvatarStorage.IsValidBusinessKey(request.Key, accountResult.Value!))
        {
            return Result<CompletePortalAvatarUploadResponse>.Failure(TenancyErrors.InvalidPortalAvatarObjectKey);
        }

        var exists = await storage.ObjectExistsAsync(MediaBucket.Covers, request.Key, cancellationToken);
        if (!exists)
            return Result<CompletePortalAvatarUploadResponse>.Failure(TenancyErrors.PortalAvatarObjectMissing);

        var profile = await profileService.GetOrCreateAsync(accountResult.Value!, cancellationToken);
        var updateResult = profile.SetAvatarObjectKey(request.Key, clock.UtcNow);
        if (!updateResult.IsSuccess)
            return Result<CompletePortalAvatarUploadResponse>.Failure(updateResult.Error!);

        await profileService.SaveChangesAsync(cancellationToken);

        var avatarUrl = mediaUrls.BuildCoverArtUrl(request.Key)!;
        return Result<CompletePortalAvatarUploadResponse>.Success(
            new CompletePortalAvatarUploadResponse(request.Key, avatarUrl));
    }
}
