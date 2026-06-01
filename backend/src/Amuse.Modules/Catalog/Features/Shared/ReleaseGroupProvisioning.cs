using Amuse.Domain.Catalog;
using Amuse.Domain.SharedKernel;
using Amuse.Domain.Tenancy;
using Amuse.Modules.Catalog.Persistence;
using Amuse.Modules.Common.Time;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Catalog.Features.Shared;

internal static class ReleaseGroupProvisioning
{
    internal static async Task<Result<ReleaseGroupId>> ResolveForNewReleaseAsync(
        CatalogDbContext db,
        IClock clock,
        OrganizationId organizationId,
        ArtistId artistId,
        string releaseTitle,
        Guid? requestedGroupId,
        CancellationToken cancellationToken)
    {
        if (requestedGroupId is { } groupGuid && groupGuid != Guid.Empty)
        {
            var typedGroupId = ReleaseGroupId.From(groupGuid);
            var group = await db.ReleaseGroups
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.Id == typedGroupId, cancellationToken);

            if (group is null)
                return Result<ReleaseGroupId>.Failure(CatalogErrors.ReleaseGroupNotFound);

            var scopeResult = CatalogScopeGuard.EnsureOrganizationScope(organizationId, group.OrganizationId);
            if (!scopeResult.IsSuccess)
                return Result<ReleaseGroupId>.Failure(scopeResult.Error!);

            if (!group.BelongsToArtist(artistId))
                return Result<ReleaseGroupId>.Failure(CatalogErrors.ReleaseGroupArtistMismatch);

            return Result<ReleaseGroupId>.Success(typedGroupId);
        }

        return await CreateForArtistAsync(
            db,
            clock,
            organizationId,
            artistId,
            releaseTitle,
            description: null,
            cancellationToken);
    }

    internal static async Task<Result<ReleaseGroupId>> CreateForArtistAsync(
        CatalogDbContext db,
        IClock clock,
        OrganizationId organizationId,
        ArtistId artistId,
        string title,
        string? description,
        CancellationToken cancellationToken)
    {
        var slugResult = await CatalogSlugHelper.AllocateUniqueReleaseGroupSlugAsync(
            db,
            artistId,
            title,
            cancellationToken);
        if (!slugResult.IsSuccess)
            return Result<ReleaseGroupId>.Failure(slugResult.Error!);

        var now = clock.UtcNow;
        var createResult = ReleaseGroup.Create(
            ReleaseGroupId.New(),
            organizationId,
            artistId,
            title,
            slugResult.Value!,
            now,
            description);

        if (!createResult.IsSuccess)
            return Result<ReleaseGroupId>.Failure(createResult.Error!);

        var group = createResult.Value!;
        db.ReleaseGroups.Add(group);
        return Result<ReleaseGroupId>.Success(group.Id);
    }

    internal static async Task<Result<ReleaseGroup>> LoadManagedForArtistAsync(
        CatalogDbContext db,
        ReleaseGroupId groupId,
        ArtistId artistId,
        OrganizationId organizationId,
        CancellationToken cancellationToken)
    {
        var group = await db.ReleaseGroups
            .FirstOrDefaultAsync(g => g.Id == groupId, cancellationToken);

        if (group is null)
            return Result<ReleaseGroup>.Failure(CatalogErrors.ReleaseGroupNotFound);

        var scopeResult = CatalogScopeGuard.EnsureOrganizationScope(organizationId, group.OrganizationId);
        if (!scopeResult.IsSuccess)
            return Result<ReleaseGroup>.Failure(scopeResult.Error!);

        if (!group.BelongsToArtist(artistId))
            return Result<ReleaseGroup>.Failure(CatalogErrors.ReleaseGroupArtistMismatch);

        return Result<ReleaseGroup>.Success(group);
    }

    internal static async Task<Result<ReleaseGroupId?>> ResolveForReleaseUpdateAsync(
        CatalogDbContext db,
        OrganizationId organizationId,
        ArtistId artistId,
        Guid? requestedGroupId,
        CancellationToken cancellationToken)
    {
        if (requestedGroupId is not { } groupGuid || groupGuid == Guid.Empty)
            return Result<ReleaseGroupId?>.Success(null);

        var loadResult = await LoadManagedForArtistAsync(
            db,
            ReleaseGroupId.From(groupGuid),
            artistId,
            organizationId,
            cancellationToken);

        if (!loadResult.IsSuccess)
            return Result<ReleaseGroupId?>.Failure(loadResult.Error!);

        return Result<ReleaseGroupId?>.Success(loadResult.Value!.Id);
    }
}
