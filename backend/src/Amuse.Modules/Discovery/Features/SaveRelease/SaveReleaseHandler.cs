using System.Security.Claims;
using Amuse.Domain.Catalog;
using Amuse.Domain.Discovery;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Catalog.Contracts;
using Amuse.Modules.Common.Time;
using Amuse.Modules.Discovery.Features.Shared;
using Amuse.Modules.Discovery.Persistence;
using Amuse.Modules.Identity.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Discovery.Features.SaveRelease;

internal sealed class SaveReleaseHandler(
    DiscoveryDbContext db,
    ICatalogDiscoveryReadModel catalog,
    IListenerPersonaReadModel personaReadModel,
    IClock clock)
{
    public async Task<Result> HandleAsync(
        Guid releaseId,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        if (releaseId == Guid.Empty)
            return Result.Failure(DiscoveryErrors.InvalidReleaseId);

        var listenerResult = await DiscoveryPrincipal.RequireListenerAsync(
            principal, personaReadModel, cancellationToken);
        if (!listenerResult.IsSuccess)
            return Result.Failure(listenerResult.Error!);

        var typedReleaseId = ReleaseId.From(releaseId);
        if (!await catalog.ReleaseExistsAndPublishedAsync(typedReleaseId, cancellationToken))
            return Result.Failure(DiscoveryErrors.InvalidReleaseId);

        var listenerId = listenerResult.Value!.ListenerProfileId;
        var entries = await db.LibraryEntries
            .Where(e => e.ListenerProfileId == listenerId)
            .ToListAsync(cancellationToken);
        var library = ListenerLibrary.Rehydrate(listenerId, entries);

        var saveResult = library.TrySaveRelease(releaseId, clock.UtcNow);
        if (!saveResult.IsSuccess)
            return Result.Failure(saveResult.Error!);

        if (saveResult.Value is not null)
            db.LibraryEntries.Add(saveResult.Value);

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
