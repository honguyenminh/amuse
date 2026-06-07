using Amuse.Domain.Catalog;
using Amuse.Domain.SharedKernel;
using Amuse.Domain.Tenancy;
using Amuse.Modules.Catalog.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Catalog.Features.Common;

internal static class CatalogAuditScopeGuard
{
    internal static async Task<Result> EnsureResourceAccessibleAsync(
        CatalogDbContext db,
        string tableName,
        Guid targetId,
        OrganizationId organizationId,
        CancellationToken cancellationToken)
    {
        if (targetId == Guid.Empty)
            return Result.Failure(CatalogErrors.Forbidden);

        return tableName switch
        {
            CatalogAuditTables.Artist => await EnsureArtistAsync(db, targetId, organizationId, cancellationToken),
            CatalogAuditTables.Release => await EnsureReleaseAsync(db, targetId, organizationId, cancellationToken),
            CatalogAuditTables.Track => await EnsureTrackAsync(db, targetId, organizationId, cancellationToken),
            CatalogAuditTables.ReleaseGroup => await EnsureReleaseGroupAsync(db, targetId, organizationId, cancellationToken),
            _ => Result.Failure(CatalogErrors.Forbidden),
        };
    }

    private static async Task<Result> EnsureArtistAsync(
        CatalogDbContext db,
        Guid targetId,
        OrganizationId organizationId,
        CancellationToken cancellationToken)
    {
        var artist = await db.Artists
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == ArtistId.From(targetId), cancellationToken);

        if (artist is null)
            return Result.Failure(CatalogErrors.ArtistNotFound);

        return CatalogScopeGuard.EnsureArtistManagedBy(artist, organizationId);
    }

    private static async Task<Result> EnsureReleaseAsync(
        CatalogDbContext db,
        Guid targetId,
        OrganizationId organizationId,
        CancellationToken cancellationToken)
    {
        var release = await db.Releases
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == ReleaseId.From(targetId), cancellationToken);

        if (release is null)
            return Result.Failure(CatalogErrors.ReleaseNotFound);

        return CatalogScopeGuard.EnsureOrganizationScope(organizationId, release.OrganizationId);
    }

    private static async Task<Result> EnsureTrackAsync(
        CatalogDbContext db,
        Guid targetId,
        OrganizationId organizationId,
        CancellationToken cancellationToken)
    {
        var track = await db.Tracks
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == TrackId.From(targetId), cancellationToken);

        if (track is null)
            return Result.Failure(CatalogErrors.TrackNotFound);

        return CatalogScopeGuard.EnsureOrganizationScope(organizationId, track.OrganizationId);
    }

    private static async Task<Result> EnsureReleaseGroupAsync(
        CatalogDbContext db,
        Guid targetId,
        OrganizationId organizationId,
        CancellationToken cancellationToken)
    {
        var group = await db.ReleaseGroups
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == ReleaseGroupId.From(targetId), cancellationToken);

        if (group is null)
            return Result.Failure(CatalogErrors.ReleaseGroupNotFound);

        var artist = await db.Artists
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == group.ArtistId, cancellationToken);

        if (artist is null)
            return Result.Failure(CatalogErrors.ReleaseGroupNotFound);

        return CatalogScopeGuard.EnsureArtistManagedBy(artist, organizationId);
    }
}
