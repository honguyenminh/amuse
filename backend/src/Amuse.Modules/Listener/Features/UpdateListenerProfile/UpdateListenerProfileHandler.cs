using System.Security.Claims;
using Amuse.Domain.Identity;
using Amuse.Domain.Listener;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Common.Time;
using Amuse.Modules.Listener.Features.Common;
using Amuse.Modules.Listener.Services;
using Amuse.Modules.Media;

namespace Amuse.Modules.Listener.Features.UpdateListenerProfile;

internal sealed class UpdateListenerProfileHandler(
    EnsureListenerProfileService ensureService,
    ListenerProfileService profileService,
    IMediaPublicUrlBuilder mediaUrls,
    IClock clock)
{
    public async Task<Result<ListenerProfileResponse>> HandleAsync(
        UpdateListenerProfileRequest request,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var accountId = ResolveAccountId(principal);
        if (accountId is null)
            return Result<ListenerProfileResponse>.Failure(IdentityErrors.InvalidRefreshToken);

        var resolvedAccountId = accountId.Value;
        await ensureService.EnsureAsync(resolvedAccountId, cancellationToken);
        var (profile, preference) = await profileService.GetForAccountAsync(resolvedAccountId, cancellationToken);

        if (request.DisplayName is not null)
        {
            var updateResult = profile.UpdatePresentation(
                request.DisplayName,
                request.AvatarAccentSeed,
                clock.UtcNow);
            if (!updateResult.IsSuccess)
                return Result<ListenerProfileResponse>.Failure(updateResult.Error!);
        }
        else if (request.AvatarAccentSeed is not null)
        {
            if (profile.DisplayName is null)
            {
                return Result<ListenerProfileResponse>.Failure(ListenerErrors.InvalidDisplayName);
            }

            var updateResult = profile.UpdatePresentation(
                profile.DisplayName,
                request.AvatarAccentSeed,
                clock.UtcNow);
            if (!updateResult.IsSuccess)
                return Result<ListenerProfileResponse>.Failure(updateResult.Error!);
        }

        if (request.ClearAvatar == true)
        {
            var clearResult = profile.SetAvatarObjectKey(null, clock.UtcNow);
            if (!clearResult.IsSuccess)
                return Result<ListenerProfileResponse>.Failure(clearResult.Error!);
        }

        if (request.AllowUnverifiedArtists is not null)
        {
            preference ??= await profileService.GetOrCreatePreferenceAsync(resolvedAccountId, cancellationToken);
            var preferenceResult = preference.SetUnverifiedPreference(
                request.AllowUnverifiedArtists.Value,
                clock.UtcNow);
            if (!preferenceResult.IsSuccess)
                return Result<ListenerProfileResponse>.Failure(preferenceResult.Error!);
        }

        await profileService.SaveChangesAsync(cancellationToken);

        return Result<ListenerProfileResponse>.Success(
            ListenerProfileMapper.ToResponse(profile, preference, mediaUrls));
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
