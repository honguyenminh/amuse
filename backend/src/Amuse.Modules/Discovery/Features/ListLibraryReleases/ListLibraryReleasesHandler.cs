using System.Security.Claims;
using Amuse.Domain.Discovery;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Catalog.Contracts;
using Amuse.Modules.Discovery.Features.Shared;
using Amuse.Modules.Discovery.Persistence;
using Amuse.Modules.Identity.Contracts;
using Amuse.Modules.Media;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Discovery.Features.ListLibraryReleases;

internal sealed class ListLibraryReleasesHandler(
    DiscoveryDbContext db,
    ICatalogDiscoveryReadModel catalog,
    IListenerPersonaReadModel personaReadModel,
    IObjectStorage storage)
{
    public async Task<Result<SavedReleasesResponse>> HandleAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var listenerResult = await DiscoveryPrincipal.RequireListenerAsync(
            principal, personaReadModel, cancellationToken);
        if (!listenerResult.IsSuccess)
            return Result<SavedReleasesResponse>.Failure(listenerResult.Error!);

        var entries = await db.LibraryEntries.AsNoTracking()
            .Where(e =>
                e.ListenerProfileId == listenerResult.Value!.ListenerProfileId
                && e.Kind == LibraryEntryKind.SavedRelease)
            .OrderByDescending(e => e.SavedAt)
            .ToListAsync(cancellationToken);

        if (entries.Count == 0)
            return Result<SavedReleasesResponse>.Success(new SavedReleasesResponse([]));

        var releaseIds = entries.Select(e => e.TargetId).ToArray();
        var summaries = await catalog.GetReleaseSummariesAsync(releaseIds, cancellationToken);

        var releases = entries
            .Where(e => summaries.ContainsKey(e.TargetId))
            .Select(e => DiscoveryMapper.ToSavedRelease(e, summaries[e.TargetId], storage))
            .ToArray();

        return Result<SavedReleasesResponse>.Success(new SavedReleasesResponse(releases));
    }
}
