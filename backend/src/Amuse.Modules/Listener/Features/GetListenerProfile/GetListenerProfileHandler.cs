using System.Security.Claims;
using Amuse.Domain.Identity;
using Amuse.Domain.Listener;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Listener.Features.Common;
using Amuse.Modules.Listener.Services;
using Amuse.Modules.Media;

namespace Amuse.Modules.Listener.Features.GetListenerProfile;

internal sealed class GetListenerProfileHandler(
    ListenerProfileService profileService,
    IMediaPublicUrlBuilder mediaUrls)
{
    public async Task<Result<ListenerProfileResponse>> HandleAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var accountId = ResolveAccountId(principal);
        if (accountId is null)
            return Result<ListenerProfileResponse>.Failure(IdentityErrors.InvalidRefreshToken);

        var profileResult = await profileService.TryGetForAccountAsync(accountId.Value, cancellationToken);
        if (!profileResult.IsSuccess)
            return Result<ListenerProfileResponse>.Failure(profileResult.Error!);

        var (profile, preference) = profileResult.Value!;
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
