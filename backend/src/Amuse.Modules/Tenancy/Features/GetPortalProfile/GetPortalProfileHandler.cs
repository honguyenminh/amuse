using System.Security.Claims;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Media;
using Amuse.Modules.Tenancy.Features.Common;
using Amuse.Modules.Tenancy.Services;

namespace Amuse.Modules.Tenancy.Features.GetPortalProfile;

internal sealed class GetPortalProfileHandler(
    BusinessPortalProfileService profileService,
    IMediaPublicUrlBuilder mediaUrls)
{
    public async Task<Result<BusinessPortalProfileResponse>> HandleAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var accountResult = TenancyAccountAccessor.GetAccountId(principal);
        if (!accountResult.IsSuccess)
            return Result<BusinessPortalProfileResponse>.Failure(accountResult.Error!);

        var profile = await profileService.GetOrCreateAsync(accountResult.Value!, cancellationToken);
        return Result<BusinessPortalProfileResponse>.Success(ToResponse(profile, mediaUrls));
    }

    internal static BusinessPortalProfileResponse ToResponse(
        Domain.Tenancy.BusinessPortalProfile profile,
        IMediaPublicUrlBuilder mediaUrls) =>
        new(
            profile.DisplayName,
            profile.AvatarAccentSeed,
            mediaUrls.BuildCoverArtUrl(profile.AvatarObjectKey),
            profile.IsComplete,
            profile.UpdatedAt);
}
