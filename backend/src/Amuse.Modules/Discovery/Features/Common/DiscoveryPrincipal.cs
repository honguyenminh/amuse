using System.Security.Claims;
using Amuse.Domain.Identity;
using Amuse.Domain.Listener;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Identity.Contracts;

namespace Amuse.Modules.Discovery.Features.Common;

internal sealed record DiscoveryListenerContext(AccountId AccountId, ListenerProfileId ListenerProfileId);

internal static class DiscoveryPrincipal
{
    public static AccountId? ResolveAccountId(ClaimsPrincipal principal)
    {
        var sub = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(sub) || !Guid.TryParse(sub, out var accountGuid))
            return null;

        return AccountId.From(accountGuid);
    }

    public static ListenerProfileId? ResolveListenerProfileId(ClaimsPrincipal principal)
    {
        var listenerId = principal.FindFirstValue("listener_id");
        if (string.IsNullOrWhiteSpace(listenerId) || !Guid.TryParse(listenerId, out var listenerGuid))
            return null;

        return ListenerProfileId.From(listenerGuid);
    }

    public static async Task<Result<DiscoveryListenerContext>> RequireListenerAsync(
        ClaimsPrincipal principal,
        IListenerPersonaReadModel personaReadModel,
        CancellationToken cancellationToken)
    {
        var accountId = ResolveAccountId(principal);
        if (accountId is null)
            return Result<DiscoveryListenerContext>.Failure(IdentityErrors.InvalidRefreshToken);

        var claimProfileId = ResolveListenerProfileId(principal);
        if (claimProfileId is not { } resolvedProfileId)
            return Result<DiscoveryListenerContext>.Failure(IdentityErrors.InvalidRefreshToken);

        var profileId = await personaReadModel.GetProfileIdForAccountAsync(accountId.Value, cancellationToken);
        if (profileId is null || profileId != resolvedProfileId)
            return Result<DiscoveryListenerContext>.Failure(IdentityErrors.InvalidRefreshToken);

        return Result<DiscoveryListenerContext>.Success(
            new DiscoveryListenerContext(accountId.Value, resolvedProfileId));
    }

    public static async Task<ListenerProfileId?> TryResolveListenerProfileIdAsync(
        ClaimsPrincipal principal,
        IListenerPersonaReadModel personaReadModel,
        CancellationToken cancellationToken)
    {
        if (principal.Identity?.IsAuthenticated != true)
            return null;

        var accountId = ResolveAccountId(principal);
        if (accountId is null)
            return null;

        return await personaReadModel.GetProfileIdForAccountAsync(accountId.Value, cancellationToken);
    }
}
