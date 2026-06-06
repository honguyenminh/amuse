using System.Security.Claims;
using Amuse.Domain.Catalog;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Catalog.Features.BrowseHome;
using Amuse.Modules.Catalog.Features.Shared;
using Amuse.Modules.Catalog.Persistence;
using Amuse.Modules.Common.Time;
using Amuse.Modules.Media;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Catalog.Features.ManageReleaseGroups;

internal sealed class CreateReleaseGroupHandler(CatalogDbContext db, IClock clock, CatalogAuditWriter auditWriter)
{
    public async Task<Result<ManageReleaseGroupResponse>> HandleAsync(
        Guid artistId,
        CreateReleaseGroupRequest request,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var orgResult = CatalogPersonaAccessor.GetOrganizationId(principal);
        if (!orgResult.IsSuccess)
            return Result<ManageReleaseGroupResponse>.Failure(orgResult.Error!);

        if (artistId == Guid.Empty)
            return Result<ManageReleaseGroupResponse>.Failure(CatalogErrors.ArtistNotFound);

        var typedArtistId = ArtistId.From(artistId);
        var artist = await db.Artists
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == typedArtistId, cancellationToken);

        if (artist is null)
            return Result<ManageReleaseGroupResponse>.Failure(CatalogErrors.ArtistNotFound);

        var artistScope = CatalogScopeGuard.EnsureArtistManagedBy(artist, orgResult.Value!);
        if (!artistScope.IsSuccess)
            return Result<ManageReleaseGroupResponse>.Failure(artistScope.Error!);

        var createResult = await ReleaseGroupProvisioning.CreateForArtistAsync(
            db,
            clock,
            orgResult.Value!,
            typedArtistId,
            request.Title,
            requestedReleaseSlug: null,
            request.Description,
            cancellationToken);

        if (!createResult.IsSuccess)
            return Result<ManageReleaseGroupResponse>.Failure(createResult.Error!);

        var group = db.ReleaseGroups.Local.Single(g => g.Id == createResult.Value!);

        await db.SaveChangesAsync(cancellationToken);

        await auditWriter.WriteCreateAsync(
            CatalogAuditTables.ReleaseGroup,
            group.Id.Value,
            CatalogAuditSnapshotMapper.FromReleaseGroup(group),
            CatalogAccountAccessor.TryGetAccountId(principal),
            cancellationToken);

        return Result<ManageReleaseGroupResponse>.Success(ReleaseGroupMapper.ToResponse(group));
    }
}

internal sealed class ListReleaseGroupsHandler(CatalogDbContext db)
{
    public async Task<Result<ManageReleaseGroupListResponse>> HandleAsync(
        Guid artistId,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var orgResult = CatalogPersonaAccessor.GetOrganizationId(principal);
        if (!orgResult.IsSuccess)
            return Result<ManageReleaseGroupListResponse>.Failure(orgResult.Error!);

        if (artistId == Guid.Empty)
            return Result<ManageReleaseGroupListResponse>.Failure(CatalogErrors.ArtistNotFound);

        var typedArtistId = ArtistId.From(artistId);
        var artist = await db.Artists
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == typedArtistId, cancellationToken);

        if (artist is null)
            return Result<ManageReleaseGroupListResponse>.Failure(CatalogErrors.ArtistNotFound);

        var artistScope = CatalogScopeGuard.EnsureArtistManagedBy(artist, orgResult.Value!);
        if (!artistScope.IsSuccess)
            return Result<ManageReleaseGroupListResponse>.Failure(artistScope.Error!);

        var items = await db.ReleaseGroups
            .AsNoTracking()
            .Where(g => g.ArtistId == typedArtistId)
            .OrderBy(g => g.Title)
            .Select(g => new ManageReleaseGroupResponse(
                g.Id.Value,
                g.Slug.Value,
                g.Title,
                g.Description,
                g.ArtistId.Value,
                g.CreatedAt,
                g.UpdatedAt))
            .ToListAsync(cancellationToken);

        return Result<ManageReleaseGroupListResponse>.Success(new ManageReleaseGroupListResponse(items));
    }
}

internal sealed class GetReleaseGroupDetailHandler(CatalogDbContext db, IObjectStorage storage)
{
    public async Task<Result<ManageReleaseGroupDetailResponse>> HandleAsync(
        Guid artistId,
        Guid releaseGroupId,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var orgResult = CatalogPersonaAccessor.GetOrganizationId(principal);
        if (!orgResult.IsSuccess)
            return Result<ManageReleaseGroupDetailResponse>.Failure(orgResult.Error!);

        if (artistId == Guid.Empty)
            return Result<ManageReleaseGroupDetailResponse>.Failure(CatalogErrors.ArtistNotFound);

        if (releaseGroupId == Guid.Empty)
            return Result<ManageReleaseGroupDetailResponse>.Failure(CatalogErrors.ReleaseGroupNotFound);

        var typedArtistId = ArtistId.From(artistId);
        var artist = await db.Artists
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == typedArtistId, cancellationToken);

        if (artist is null)
            return Result<ManageReleaseGroupDetailResponse>.Failure(CatalogErrors.ArtistNotFound);

        var artistScope = CatalogScopeGuard.EnsureArtistManagedBy(artist, orgResult.Value!);
        if (!artistScope.IsSuccess)
            return Result<ManageReleaseGroupDetailResponse>.Failure(artistScope.Error!);

        var loadResult = await ReleaseGroupProvisioning.LoadManagedForArtistAsync(
            db,
            ReleaseGroupId.From(releaseGroupId),
            typedArtistId,
            orgResult.Value!,
            cancellationToken);

        if (!loadResult.IsSuccess)
            return Result<ManageReleaseGroupDetailResponse>.Failure(loadResult.Error!);

        var group = loadResult.Value!;
        var releases = await db.Releases
            .AsNoTracking()
            .Where(r => r.ReleaseGroupId == group.Id && r.ArtistId == typedArtistId)
            .OrderByDescending(r => r.ReleaseDate)
            .Select(r => new ManageReleaseGroupMemberResponse(
                r.Id.Value,
                r.Slug.Value,
                r.Title,
                r.ReleaseType,
                r.LifecycleStatus,
                r.ReleaseDate,
                BrowseHomeHandler.CoverArtUrlFor(storage, r.CoverArtKey)))
            .ToListAsync(cancellationToken);

        return Result<ManageReleaseGroupDetailResponse>.Success(
            new ManageReleaseGroupDetailResponse(
                group.Id.Value,
                group.Slug.Value,
                group.Title,
                group.Description,
                group.ArtistId.Value,
                artist.Name,
                group.CreatedAt,
                group.UpdatedAt,
                releases));
    }
}

internal sealed class UpdateReleaseGroupHandler(CatalogDbContext db, IClock clock, CatalogAuditWriter auditWriter)
{
    public async Task<Result<ManageReleaseGroupResponse>> HandleAsync(
        Guid artistId,
        Guid releaseGroupId,
        UpdateReleaseGroupRequest request,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var orgResult = CatalogPersonaAccessor.GetOrganizationId(principal);
        if (!orgResult.IsSuccess)
            return Result<ManageReleaseGroupResponse>.Failure(orgResult.Error!);

        if (artistId == Guid.Empty)
            return Result<ManageReleaseGroupResponse>.Failure(CatalogErrors.ArtistNotFound);

        if (releaseGroupId == Guid.Empty)
            return Result<ManageReleaseGroupResponse>.Failure(CatalogErrors.ReleaseGroupNotFound);

        var loadResult = await ReleaseGroupProvisioning.LoadManagedForArtistAsync(
            db,
            ReleaseGroupId.From(releaseGroupId),
            ArtistId.From(artistId),
            orgResult.Value!,
            cancellationToken);

        if (!loadResult.IsSuccess)
            return Result<ManageReleaseGroupResponse>.Failure(loadResult.Error!);

        var group = loadResult.Value!;
        var before = CatalogAuditSnapshotMapper.FromReleaseGroup(group);
        var updateResult = group.UpdateMetadata(request.Title, request.Description, clock.UtcNow);
        if (!updateResult.IsSuccess)
            return Result<ManageReleaseGroupResponse>.Failure(updateResult.Error!);

        await db.SaveChangesAsync(cancellationToken);

        await auditWriter.WriteUpdateAsync(
            CatalogAuditTables.ReleaseGroup,
            group.Id.Value,
            before,
            CatalogAuditSnapshotMapper.FromReleaseGroup(group),
            CatalogAccountAccessor.TryGetAccountId(principal),
            cancellationToken);

        return Result<ManageReleaseGroupResponse>.Success(ReleaseGroupMapper.ToResponse(group));
    }
}

internal static class ReleaseGroupMapper
{
    internal static ManageReleaseGroupResponse ToResponse(ReleaseGroup group) =>
        new(
            group.Id.Value,
            group.Slug.Value,
            group.Title,
            group.Description,
            group.ArtistId.Value,
            group.CreatedAt,
            group.UpdatedAt);
}
