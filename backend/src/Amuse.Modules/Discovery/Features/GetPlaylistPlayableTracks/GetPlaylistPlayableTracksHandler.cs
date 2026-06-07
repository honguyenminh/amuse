using System.Security.Claims;
using Amuse.Domain.Discovery;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Discovery.Features.Shared;
namespace Amuse.Modules.Discovery.Features.GetPlaylistPlayableTracks;

internal sealed class GetPlaylistPlayableTracksHandler(
    PlayableCollectionResolver resolver,
    PlaylistViewContextBuilder viewContextBuilder)
{
    public async Task<Result<PlayableTracksResponse>> HandleAsync(
        Guid playlistId,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        if (playlistId == Guid.Empty)
            return Result<PlayableTracksResponse>.Failure(DiscoveryErrors.PlaylistNotFound);

        var viewContext = await viewContextBuilder.BuildAsync(principal, cancellationToken);
        return await resolver.ResolvePlaylistTracksAsync(
            PlaylistId.From(playlistId),
            viewContext,
            cancellationToken);
    }
}
