using System.Security.Claims;
using Amuse.Domain.Discovery;
using Amuse.Domain.Identity;
using Amuse.Domain.Listener;
using Amuse.Modules.Identity.Contracts;
using Amuse.Modules.Tenancy.Contracts;

namespace Amuse.Modules.Discovery.Features.Shared;

internal sealed class PlaylistViewContextBuilder(
    IListenerPersonaReadModel personaReadModel,
    IAccountEmailLookup accountEmailLookup)
{
    public async Task<PlaylistViewContext> BuildAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var profileId = await DiscoveryPrincipal.TryResolveListenerProfileIdAsync(
            principal,
            personaReadModel,
            cancellationToken);

        string? emailNormalized = null;
        var accountId = DiscoveryPrincipal.ResolveAccountId(principal);
        if (accountId is not null)
        {
            var email = await accountEmailLookup.GetEmailAsync(accountId.Value, cancellationToken);
            if (!string.IsNullOrWhiteSpace(email))
            {
                var emailResult = ShareGrantEmail.TryCreate(email);
                emailNormalized = emailResult.IsSuccess
                    ? emailResult.Value.Value
                    : email.Trim().ToLowerInvariant();
            }
        }

        return new PlaylistViewContext(profileId, emailNormalized);
    }

    public async Task<PlaylistViewContext> BuildForListenerAsync(
        ListenerProfileId listenerProfileId,
        AccountId accountId,
        CancellationToken cancellationToken)
    {
        string? emailNormalized = null;
        var email = await accountEmailLookup.GetEmailAsync(accountId, cancellationToken);
        if (!string.IsNullOrWhiteSpace(email))
        {
            var emailResult = ShareGrantEmail.TryCreate(email);
            emailNormalized = emailResult.IsSuccess
                ? emailResult.Value.Value
                : email.Trim().ToLowerInvariant();
        }

        return new PlaylistViewContext(listenerProfileId, emailNormalized);
    }
}
