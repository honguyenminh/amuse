using System.Security.Claims;
using Amuse.Domain.Discovery;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Discovery.Features.Shared;
using Amuse.Modules.Discovery.Persistence;
using Amuse.Modules.Identity.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Discovery.Features.UnsaveRelease;

internal sealed class UnsaveReleaseHandler(
    DiscoveryDbContext db,
    IListenerPersonaReadModel personaReadModel)
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

        var entry = await db.LibraryEntries.FirstOrDefaultAsync(
            e => e.ListenerProfileId == listenerResult.Value!.ListenerProfileId
                 && e.Kind == LibraryEntryKind.SavedRelease
                 && e.TargetId == releaseId,
            cancellationToken);

        if (entry is null)
            return Result.Failure(DiscoveryErrors.LibraryEntryNotFound);

        db.LibraryEntries.Remove(entry);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
