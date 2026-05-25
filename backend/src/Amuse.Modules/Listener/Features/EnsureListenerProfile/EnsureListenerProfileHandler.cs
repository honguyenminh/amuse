using System.Security.Claims;
using Amuse.Domain.Identity;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Listener.Services;

namespace Amuse.Modules.Listener.Features.EnsureListenerProfile;

internal sealed class EnsureListenerProfileHandler(EnsureListenerProfileService ensureService)
{
    public async Task<Result<EnsureListenerProfileResponse>> HandleAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var sub = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(sub) || !Guid.TryParse(sub, out var accountGuid))
            return Result<EnsureListenerProfileResponse>.Failure(IdentityErrors.InvalidRefreshToken);

        var accountId = AccountId.From(accountGuid);
        var existingId = await ensureService.GetProfileIdForAccountAsync(accountId, cancellationToken);
        var profile = await ensureService.EnsureAsync(accountId, cancellationToken);

        return Result<EnsureListenerProfileResponse>.Success(new EnsureListenerProfileResponse(
            profile.Id.Value,
            existingId is null));
    }
}

public sealed record EnsureListenerProfileResponse(Guid ListenerId, bool Created);
