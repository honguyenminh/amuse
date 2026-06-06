using System.Security.Claims;
using Amuse.Domain.Identity;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Listener.Features.Shared;
using Amuse.Modules.Listener.Services;
using Amuse.Modules.Media;

namespace Amuse.Modules.Listener.Features.GetListenerProfile;

internal sealed class GetListenerProfileHandler(
    EnsureListenerProfileService ensureService,
    ListenerProfileService profileService,
    IObjectStorage storage)
{
    public async Task<Result<ListenerProfileResponse>> HandleAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var accountId = ResolveAccountId(principal);
        if (accountId is null)
            return Result<ListenerProfileResponse>.Failure(IdentityErrors.InvalidRefreshToken);

        var resolvedAccountId = accountId.Value;
        await ensureService.EnsureAsync(resolvedAccountId, cancellationToken);
        var (profile, preference) = await profileService.GetForAccountAsync(resolvedAccountId, cancellationToken);
        return Result<ListenerProfileResponse>.Success(
            ListenerProfileMapper.ToResponse(profile, preference, storage));
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
