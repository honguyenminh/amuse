using Amuse.Domain.SharedKernel;
using Amuse.Modules.Discovery.Features.Common;

namespace Amuse.Modules.Discovery.Features.GetReleasePlayableTracks;

internal sealed class GetReleasePlayableTracksHandler(PlayableCollectionResolver resolver)
{
    public Task<Result<PlayableTracksResponse>> HandleAsync(
        Guid releaseId,
        CancellationToken cancellationToken) =>
        resolver.ResolveReleaseTracksAsync(releaseId, cancellationToken);
}
