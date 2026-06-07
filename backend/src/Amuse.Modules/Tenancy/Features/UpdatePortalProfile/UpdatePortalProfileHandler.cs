using System.Security.Claims;
using Amuse.Domain.SharedKernel;
using Amuse.Domain.Tenancy;
using Amuse.Modules.Common.Time;
using Amuse.Modules.Media;
using Amuse.Modules.Tenancy.Features.GetPortalProfile;
using Amuse.Modules.Tenancy.Features.Shared;
using Amuse.Modules.Tenancy.Services;

namespace Amuse.Modules.Tenancy.Features.UpdatePortalProfile;

internal sealed class UpdatePortalProfileHandler(
    BusinessPortalProfileService profileService,
    IMediaPublicUrlBuilder mediaUrls,
    IClock clock)
{
    public async Task<Result<BusinessPortalProfileResponse>> HandleAsync(
        UpdateBusinessPortalProfileRequest request,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var accountResult = TenancyAccountAccessor.GetAccountId(principal);
        if (!accountResult.IsSuccess)
            return Result<BusinessPortalProfileResponse>.Failure(accountResult.Error!);

        var profile = await profileService.GetOrCreateAsync(accountResult.Value!, cancellationToken);

        if (request.DisplayName is not null)
        {
            var updateResult = profile.UpdatePresentation(
                request.DisplayName,
                request.AvatarAccentSeed,
                clock.UtcNow);
            if (!updateResult.IsSuccess)
                return Result<BusinessPortalProfileResponse>.Failure(updateResult.Error!);
        }
        else if (request.AvatarAccentSeed is not null)
        {
            if (profile.DisplayName is null)
            {
                return Result<BusinessPortalProfileResponse>.Failure(TenancyErrors.InvalidDisplayName);
            }

            var updateResult = profile.UpdatePresentation(
                profile.DisplayName,
                request.AvatarAccentSeed,
                clock.UtcNow);
            if (!updateResult.IsSuccess)
                return Result<BusinessPortalProfileResponse>.Failure(updateResult.Error!);
        }

        if (request.ClearAvatar == true)
        {
            var clearResult = profile.SetAvatarObjectKey(null, clock.UtcNow);
            if (!clearResult.IsSuccess)
                return Result<BusinessPortalProfileResponse>.Failure(clearResult.Error!);
        }

        await profileService.SaveChangesAsync(cancellationToken);
        return Result<BusinessPortalProfileResponse>.Success(
            GetPortalProfileHandler.ToResponse(profile, mediaUrls));
    }
}
