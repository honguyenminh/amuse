using System.Security.Claims;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Catalog.Features.BrowseHome;
using Amuse.Modules.Media;
using Amuse.Modules.Tenancy.Features.Shared;
using Amuse.Modules.Tenancy.Services;

namespace Amuse.Modules.Tenancy.Features.GetPortalProfile;

internal sealed class GetPortalProfileHandler(
    BusinessPortalProfileService profileService,
    IObjectStorage storage)
{
    public async Task<Result<BusinessPortalProfileResponse>> HandleAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var accountResult = TenancyAccountAccessor.GetAccountId(principal);
        if (!accountResult.IsSuccess)
            return Result<BusinessPortalProfileResponse>.Failure(accountResult.Error!);

        var profile = await profileService.GetOrCreateAsync(accountResult.Value!, cancellationToken);
        return Result<BusinessPortalProfileResponse>.Success(ToResponse(profile, storage));
    }

    internal static BusinessPortalProfileResponse ToResponse(
        Domain.Tenancy.BusinessPortalProfile profile,
        IObjectStorage storage) =>
        new(
            profile.DisplayName,
            profile.AvatarAccentSeed,
            string.IsNullOrEmpty(profile.AvatarObjectKey)
                ? null
                : BrowseHomeHandler.CoverArtUrlFor(storage, profile.AvatarObjectKey),
            profile.IsComplete,
            profile.UpdatedAt);
}
