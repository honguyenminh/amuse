using System.Security.Claims;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Discovery.Features.Common;
using Amuse.Modules.Identity.Contracts;

namespace Amuse.Modules.Discovery.Features.GetLikedPlayableTracks;

internal sealed class GetLikedPlayableTracksHandler(
    IListenerPersonaReadModel personaReadModel,
    PlayableCollectionResolver resolver)
{
    public async Task<Result<PlayableTracksResponse>> HandleAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var listenerResult = await DiscoveryPrincipal.RequireListenerAsync(
            principal, personaReadModel, cancellationToken);
        if (!listenerResult.IsSuccess)
            return Result<PlayableTracksResponse>.Failure(listenerResult.Error!);

        return await resolver.ResolveLikedTracksAsync(
            listenerResult.Value!.ListenerProfileId,
            cancellationToken);
    }
}
