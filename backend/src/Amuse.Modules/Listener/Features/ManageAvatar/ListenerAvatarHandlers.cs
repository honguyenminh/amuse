using System.Security.Claims;
using Amuse.Domain.Identity;
using Amuse.Domain.Listener;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Common.Time;
using Amuse.Modules.Listener.Features.Common;
using Amuse.Modules.Listener.Services;
using Amuse.Modules.Media;
using Amuse.Modules.Media.Options;
using Microsoft.Extensions.Options;

namespace Amuse.Modules.Listener.Features.ManageAvatar;

internal sealed class PresignListenerAvatarUploadHandler(
    EnsureListenerProfileService ensureService,
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

    public async Task<Result<PresignListenerAvatarUploadResponse>> HandleAsync(
        PresignListenerAvatarUploadRequest request,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var accountId = ResolveAccountId(principal);
        if (accountId is null)
            return Result<PresignListenerAvatarUploadResponse>.Failure(IdentityErrors.InvalidRefreshToken);

        if (string.IsNullOrWhiteSpace(request.FileName) || string.IsNullOrWhiteSpace(request.ContentType))
            return Result<PresignListenerAvatarUploadResponse>.Failure(ListenerErrors.InvalidAvatarUploadRequest);

        if (!AllowedContentTypes.Contains(request.ContentType))
            return Result<PresignListenerAvatarUploadResponse>.Failure(ListenerErrors.InvalidAvatarUploadRequest);

        await ensureService.EnsureAsync(accountId.Value, cancellationToken);

        var ext = ExtensionFromContentType(request.ContentType, request.FileName);
        var key = $"{ProfileAvatarStorage.ListenerPrefix(accountId.Value)}{Guid.CreateVersion7()}{ext}";
        var ttl = TimeSpan.FromMinutes(mediaOptions.Value.SignedUrlMinutes);
        var expiresAt = clock.UtcNow.Add(ttl);
        var url = storage.GetSignedUploadUrl(MediaBucket.Covers, key, ttl, request.ContentType);

        return Result<PresignListenerAvatarUploadResponse>.Success(
            new PresignListenerAvatarUploadResponse(key, url, expiresAt, "PUT"));
    }

    private static AccountId? ResolveAccountId(ClaimsPrincipal principal)
    {
        var sub = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(sub) || !Guid.TryParse(sub, out var accountGuid))
            return null;

        return AccountId.From(accountGuid);
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

internal sealed class CompleteListenerAvatarUploadHandler(
    EnsureListenerProfileService ensureService,
    ListenerProfileService profileService,
    IObjectStorage storage,
    IMediaPublicUrlBuilder mediaUrls,
    IClock clock)
{
    public async Task<Result<CompleteListenerAvatarUploadResponse>> HandleAsync(
        CompleteListenerAvatarUploadRequest request,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var accountId = ResolveAccountId(principal);
        if (accountId is null)
            return Result<CompleteListenerAvatarUploadResponse>.Failure(IdentityErrors.InvalidRefreshToken);

        if (string.IsNullOrWhiteSpace(request.Key)
            || !ProfileAvatarStorage.IsValidListenerKey(request.Key, accountId.Value))
        {
            return Result<CompleteListenerAvatarUploadResponse>.Failure(ListenerErrors.InvalidAvatarObjectKey);
        }

        var exists = await storage.ObjectExistsAsync(MediaBucket.Covers, request.Key, cancellationToken);
        if (!exists)
            return Result<CompleteListenerAvatarUploadResponse>.Failure(ListenerErrors.AvatarObjectMissing);

        await ensureService.EnsureAsync(accountId.Value, cancellationToken);
        var (profile, preference) = await profileService.GetForAccountAsync(accountId.Value, cancellationToken);

        var updateResult = profile.SetAvatarObjectKey(request.Key, clock.UtcNow);
        if (!updateResult.IsSuccess)
            return Result<CompleteListenerAvatarUploadResponse>.Failure(updateResult.Error!);

        await profileService.SaveChangesAsync(cancellationToken);

        var avatarUrl = ListenerProfileMapper.AvatarUrlFor(mediaUrls, request.Key)!;
        return Result<CompleteListenerAvatarUploadResponse>.Success(
            new CompleteListenerAvatarUploadResponse(request.Key, avatarUrl));
    }

    private static AccountId? ResolveAccountId(ClaimsPrincipal principal)
    {
        var sub = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(sub) || !Guid.TryParse(sub, out var accountGuid))
            return null;

        return AccountId.From(accountGuid);
    }
}
