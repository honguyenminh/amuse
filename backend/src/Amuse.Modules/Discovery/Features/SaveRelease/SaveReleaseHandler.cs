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

        var exists = await db.LibraryEntries.AnyAsync(
            e => e.ListenerProfileId == listenerResult.Value!.ListenerProfileId
                 && e.Kind == LibraryEntryKind.SavedRelease
                 && e.TargetId == releaseId,
            cancellationToken);
        if (exists)
            return Result.Success();

        var entry = LibraryEntry.CreateSavedRelease(
            listenerResult.Value.ListenerProfileId,
            releaseId,
            clock.UtcNow);
        db.LibraryEntries.Add(entry);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
