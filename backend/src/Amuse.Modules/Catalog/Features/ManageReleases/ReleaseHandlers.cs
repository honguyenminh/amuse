using System.Security.Claims;
using Amuse.Domain.Catalog;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Catalog.Features.BrowseHome;
using Amuse.Modules.Catalog.Features.Common;
using Amuse.Modules.Catalog.Features.Common;
using Amuse.Modules.Catalog.Persistence;
using Amuse.Modules.Common.Time;
using Amuse.Modules.Media;
using Microsoft.EntityFrameworkCore;
using CatalogSlugHelper = Amuse.Modules.Catalog.Features.Common.CatalogSlugHelper;

namespace Amuse.Modules.Catalog.Features.ManageReleases;

internal sealed class CreateReleaseHandler(CatalogDbContext db, IClock clock, CatalogAuditWriter auditWriter)
{
    public async Task<Result<ManageReleaseDetailResponse>> HandleAsync(
        Guid artistId,
        CreateReleaseRequest request,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var orgResult = CatalogPersonaAccessor.GetOrganizationId(principal);
        if (!orgResult.IsSuccess)
            return Result<ManageReleaseDetailResponse>.Failure(orgResult.Error!);

        if (artistId == Guid.Empty)
            return Result<ManageReleaseDetailResponse>.Failure(CatalogErrors.ArtistNotFound);

        var organizationId = orgResult.Value!;
        var typedArtistId = ArtistId.From(artistId);

        var artist = await db.Artists
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == typedArtistId, cancellationToken);

        if (artist is null)
            return Result<ManageReleaseDetailResponse>.Failure(CatalogErrors.ArtistNotFound);

        var artistScope = CatalogScopeGuard.EnsureArtistManagedBy(artist, organizationId);
        if (!artistScope.IsSuccess)
            return Result<ManageReleaseDetailResponse>.Failure(artistScope.Error!);

        ReleaseGroupId? releaseGroupId = null;
        var groupResult = await ReleaseGroupProvisioning.ResolveForNewReleaseAsync(
            db,
            clock,
            organizationId,
            typedArtistId,
            request.Title,
            request.Slug,
            request.ReleaseGroupId,
            cancellationToken);
        if (!groupResult.IsSuccess)
            return Result<ManageReleaseDetailResponse>.Failure(groupResult.Error!);

        releaseGroupId = groupResult.Value!;

        var slugResult = await CatalogSlugHelper.ResolveReleaseSlugForCreateAsync(
            db,
            typedArtistId,
            request.Title,
            request.Slug,
            cancellationToken);
        if (!slugResult.IsSuccess)
            return Result<ManageReleaseDetailResponse>.Failure(slugResult.Error!);

        var now = clock.UtcNow;
        var createResult = Release.Create(
            ReleaseId.New(),
            organizationId,
            typedArtistId,
            request.Title,
            slugResult.Value!,
            request.ReleaseType,
            request.ReleaseDate,
            now,
            releaseGroupId: releaseGroupId,
            description: request.Description,
            upc: request.Upc,
            primaryGenre: request.PrimaryGenre,
            tags: request.Tags,
            languageCode: request.LanguageCode,
            labelName: request.LabelName,
            pLine: request.PLine,
            cLine: request.CLine,
            originalReleaseDate: request.OriginalReleaseDate,
            metadataComplete: request.MetadataComplete);

        if (!createResult.IsSuccess)
            return Result<ManageReleaseDetailResponse>.Failure(createResult.Error!);

        var release = createResult.Value!;
        db.Releases.Add(release);

        var collaboratorIdsResult = await ReleaseCollaboratorSync.ResolveArtistIdsAsync(
            db,
            request.CollaboratorArtistIds,
            cancellationToken);
        if (!collaboratorIdsResult.IsSuccess)
            return Result<ManageReleaseDetailResponse>.Failure(collaboratorIdsResult.Error!);

        var replaceResult = release.ReplaceCollaborators(collaboratorIdsResult.Value!);
        if (!replaceResult.IsSuccess)
            return Result<ManageReleaseDetailResponse>.Failure(replaceResult.Error!);

        await db.SaveChangesAsync(cancellationToken);

        await auditWriter.WriteCreateAsync(
            CatalogAuditTables.Release,
            release.Id.Value,
            CatalogAuditSnapshotMapper.FromRelease(release),
            CatalogAccountAccessor.TryGetAccountId(principal),
            cancellationToken);

        var groupDisplay = await ReleaseGroupLookup.LoadDisplayAsync(db, release.ReleaseGroupId, cancellationToken);
        var collaborators = await ReleaseCollaboratorSync.LoadAsync(db, release.Id, cancellationToken);

        return Result<ManageReleaseDetailResponse>.Success(
            ReleaseMapper.ToDetail(
                release,
                artist.Name,
                null,
                collaborators,
                groupDisplay.Title,
                groupDisplay.Slug));
    }
}

internal sealed class ListReleasesHandler(CatalogDbContext db, IMediaPublicUrlBuilder mediaUrls)
{
    public async Task<Result<ManageReleaseListResponse>> HandleAsync(
        ReleaseLifecycleStatus? status,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var orgResult = CatalogPersonaAccessor.GetOrganizationId(principal);
        if (!orgResult.IsSuccess)
            return Result<ManageReleaseListResponse>.Failure(orgResult.Error!);

        var organizationId = orgResult.Value!;

        var query = db.Releases
            .AsNoTracking()
            .Where(r => r.OrganizationId == organizationId);

        if (status.HasValue)
            query = query.Where(r => r.LifecycleStatus == status.Value);

        var rows = await query
            .OrderByDescending(r => r.ReleaseDate)
            .Join(
                db.Artists.AsNoTracking(),
                release => release.ArtistId,
                artist => artist.Id,
                (release, artist) => new
                {
                    Release = release,
                    ArtistName = artist.Name,
                })
            .ToListAsync(cancellationToken);

        var groupIds = rows
            .Where(row => row.Release.ReleaseGroupId is not null)
            .Select(row => row.Release.ReleaseGroupId!)
            .Distinct()
            .ToList();

        var groupsById = groupIds.Count == 0
            ? new Dictionary<ReleaseGroupId, (string Title, string Slug)>()
            : await db.ReleaseGroups
                .AsNoTracking()
                .Where(g => groupIds.Contains(g.Id))
                .Select(g => new { g.Id, g.Title, Slug = g.Slug.Value })
                .ToDictionaryAsync(g => g.Id, g => (g.Title, g.Slug), cancellationToken);

        var items = rows
            .Select(row =>
            {
                string? groupTitle = null;
                string? groupSlug = null;
                if (row.Release.ReleaseGroupId is { } groupId &&
                    groupsById.TryGetValue(groupId, out var group))
                {
                    groupTitle = group.Title;
                    groupSlug = group.Slug;
                }

                return ReleaseMapper.ToSummary(
                    row.Release,
                    row.ArtistName,
                    mediaUrls.BuildCoverArtUrl(row.Release.CoverArtKey),
                    groupTitle,
                    groupSlug);
            })
            .ToList();

        return Result<ManageReleaseListResponse>.Success(new ManageReleaseListResponse(items));
    }
}

internal sealed class GetReleaseHandler(CatalogDbContext db, IMediaPublicUrlBuilder mediaUrls)
{
    public async Task<Result<ManageReleaseDetailResponse>> HandleAsync(
        Guid releaseId,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var orgResult = CatalogPersonaAccessor.GetOrganizationId(principal);
        if (!orgResult.IsSuccess)
            return Result<ManageReleaseDetailResponse>.Failure(orgResult.Error!);

        if (releaseId == Guid.Empty)
            return Result<ManageReleaseDetailResponse>.Failure(CatalogErrors.ReleaseNotFound);

        var typedId = ReleaseId.From(releaseId);
        var release = await db.Releases
            .AsNoTracking()
            .Include(r => r.Tracks)
            .FirstOrDefaultAsync(r => r.Id == typedId, cancellationToken);

        if (release is null)
            return Result<ManageReleaseDetailResponse>.Failure(CatalogErrors.ReleaseNotFound);

        var scopeResult = CatalogScopeGuard.EnsureOrganizationScope(orgResult.Value!, release.OrganizationId);
        if (!scopeResult.IsSuccess)
            return Result<ManageReleaseDetailResponse>.Failure(scopeResult.Error!);

        var artistName = await db.Artists
            .AsNoTracking()
            .Where(a => a.Id == release.ArtistId)
            .Select(a => a.Name)
            .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;

        var collaborators = await ReleaseCollaboratorSync.LoadAsync(
            db,
            release.Id,
            cancellationToken);

        var groupDisplay = await ReleaseGroupLookup.LoadDisplayAsync(db, release.ReleaseGroupId, cancellationToken);

        return Result<ManageReleaseDetailResponse>.Success(
            ReleaseMapper.ToDetail(
                release,
                artistName,
                mediaUrls.BuildCoverArtUrl(release.CoverArtKey),
                collaborators,
                groupDisplay.Title,
                groupDisplay.Slug));
    }
}

internal sealed class UpdateReleaseHandler(
    CatalogDbContext db,
    IMediaPublicUrlBuilder mediaUrls,
    IClock clock,
    CatalogAuditWriter auditWriter)
{
    public async Task<Result<ManageReleaseDetailResponse>> HandleAsync(
        Guid releaseId,
        UpdateReleaseRequest request,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var orgResult = CatalogPersonaAccessor.GetOrganizationId(principal);
        if (!orgResult.IsSuccess)
            return Result<ManageReleaseDetailResponse>.Failure(orgResult.Error!);

        if (releaseId == Guid.Empty)
            return Result<ManageReleaseDetailResponse>.Failure(CatalogErrors.ReleaseNotFound);

        var typedId = ReleaseId.From(releaseId);
        var release = await db.Releases
            .Include(r => r.Tracks)
            .Include(r => r.Collaborators)
            .FirstOrDefaultAsync(r => r.Id == typedId, cancellationToken);

        if (release is null)
            return Result<ManageReleaseDetailResponse>.Failure(CatalogErrors.ReleaseNotFound);

        var scopeResult = CatalogScopeGuard.EnsureOrganizationScope(orgResult.Value!, release.OrganizationId);
        if (!scopeResult.IsSuccess)
            return Result<ManageReleaseDetailResponse>.Failure(scopeResult.Error!);

        var before = CatalogAuditSnapshotMapper.FromRelease(release);

        ReleaseGroupId? releaseGroupId;
        var groupResult = await ReleaseGroupProvisioning.ResolveForReleaseUpdateAsync(
            db,
            orgResult.Value!,
            release.ArtistId,
            request.ReleaseGroupId,
            cancellationToken);
        if (!groupResult.IsSuccess)
            return Result<ManageReleaseDetailResponse>.Failure(groupResult.Error!);

        releaseGroupId = groupResult.Value;

        if (!string.IsNullOrWhiteSpace(request.Slug))
        {
            var slugResult = await CatalogSlugHelper.EnsureAvailableReleaseSlugAsync(
                db,
                release.ArtistId,
                request.Slug,
                release.Id,
                cancellationToken);
            if (!slugResult.IsSuccess)
                return Result<ManageReleaseDetailResponse>.Failure(slugResult.Error!);

            if (slugResult.Value! != release.Slug)
            {
                var slugUpdateResult = release.UpdateSlug(slugResult.Value!, clock.UtcNow);
                if (!slugUpdateResult.IsSuccess)
                    return Result<ManageReleaseDetailResponse>.Failure(slugUpdateResult.Error!);
            }
        }

        var updateResult = release.UpdateMetadata(
            request.Title,
            request.ReleaseType,
            request.ReleaseDate,
            releaseGroupId,
            request.Description,
            request.Upc,
            request.PrimaryGenre,
            request.Tags,
            request.LanguageCode,
            request.LabelName,
            request.PLine,
            request.CLine,
            request.OriginalReleaseDate,
            request.MetadataComplete,
            clock.UtcNow);

        if (!updateResult.IsSuccess)
            return Result<ManageReleaseDetailResponse>.Failure(updateResult.Error!);

        var collaboratorIdsResult = await ReleaseCollaboratorSync.ResolveArtistIdsAsync(
            db,
            request.CollaboratorArtistIds,
            cancellationToken);
        if (!collaboratorIdsResult.IsSuccess)
            return Result<ManageReleaseDetailResponse>.Failure(collaboratorIdsResult.Error!);

        var replaceResult = release.ReplaceCollaborators(collaboratorIdsResult.Value!);
        if (!replaceResult.IsSuccess)
            return Result<ManageReleaseDetailResponse>.Failure(replaceResult.Error!);

        await db.SaveChangesAsync(cancellationToken);

        await auditWriter.WriteUpdateAsync(
            CatalogAuditTables.Release,
            release.Id.Value,
            before,
            CatalogAuditSnapshotMapper.FromRelease(release),
            CatalogAccountAccessor.TryGetAccountId(principal),
            cancellationToken);

        var artistName = await db.Artists
            .AsNoTracking()
            .Where(a => a.Id == release.ArtistId)
            .Select(a => a.Name)
            .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;

        var groupDisplay = await ReleaseGroupLookup.LoadDisplayAsync(db, release.ReleaseGroupId, cancellationToken);
        var collaborators = await ReleaseCollaboratorSync.LoadAsync(db, release.Id, cancellationToken);

        return Result<ManageReleaseDetailResponse>.Success(
            ReleaseMapper.ToDetail(
                release,
                artistName,
                mediaUrls.BuildCoverArtUrl(release.CoverArtKey),
                collaborators,
                groupDisplay.Title,
                groupDisplay.Slug));
    }
}

internal sealed class DeleteReleaseHandler(
    CatalogDbContext db,
    IObjectStorage storage,
    CatalogAuditWriter auditWriter)
{
    public async Task<Result> HandleAsync(
        Guid releaseId,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var orgResult = CatalogPersonaAccessor.GetOrganizationId(principal);
        if (!orgResult.IsSuccess)
            return Result.Failure(orgResult.Error!);

        if (releaseId == Guid.Empty)
            return Result.Failure(CatalogErrors.ReleaseNotFound);

        var typedId = ReleaseId.From(releaseId);
        var release = await db.Releases
            .Include(r => r.Tracks)
            .FirstOrDefaultAsync(r => r.Id == typedId, cancellationToken);

        if (release is null)
            return Result.Failure(CatalogErrors.ReleaseNotFound);

        var scopeResult = CatalogScopeGuard.EnsureOrganizationScope(orgResult.Value!, release.OrganizationId);
        if (!scopeResult.IsSuccess)
            return Result.Failure(scopeResult.Error!);

        if (!release.CanBeDeleted())
            return Result.Failure(CatalogErrors.ReleaseNotDeletable);

        var before = CatalogAuditSnapshotMapper.FromRelease(release);
        await CatalogMediaCleanup.DeleteReleaseMediaAsync(db, storage, release, cancellationToken);
        db.Releases.Remove(release);
        await db.SaveChangesAsync(cancellationToken);

        await auditWriter.WriteDeleteAsync(
            CatalogAuditTables.Release,
            release.Id.Value,
            before,
            CatalogAccountAccessor.TryGetAccountId(principal),
            cancellationToken);

        return Result.Success();
    }
}

internal static class ReleaseMapper
{
    internal static ManageReleaseSummaryResponse ToSummary(
        Release release,
        string artistName,
        string? coverArtUrl,
        string? releaseGroupTitle = null,
        string? releaseGroupSlug = null) =>
        new(
            release.Id.Value,
            release.Slug.Value,
            release.Title,
            release.ArtistId.Value,
            artistName,
            release.ReleaseType,
            release.LifecycleStatus,
            release.ReleaseDate,
            release.ReleaseGroupId?.Value,
            releaseGroupTitle,
            releaseGroupSlug,
            release.Description,
            release.Upc,
            release.PrimaryGenre,
            release.Tags,
            release.LanguageCode,
            release.LabelName,
            release.PLine,
            release.CLine,
            release.OriginalReleaseDate,
            release.MetadataComplete,
            coverArtUrl,
            release.CreatedAt,
            release.UpdatedAt);

    internal static ManageReleaseDetailResponse ToDetail(
        Release release,
        string artistName,
        string? coverArtUrl,
        IReadOnlyList<ManageReleaseCollaboratorResponse> collaborators,
        string? releaseGroupTitle = null,
        string? releaseGroupSlug = null) =>
        new(
            release.Id.Value,
            release.Slug.Value,
            release.Title,
            release.ArtistId.Value,
            artistName,
            release.ReleaseType,
            release.LifecycleStatus,
            release.ReleaseDate,
            release.ReleaseGroupId?.Value,
            releaseGroupTitle,
            releaseGroupSlug,
            release.Description,
            release.Upc,
            release.PrimaryGenre,
            release.Tags,
            release.LanguageCode,
            release.LabelName,
            release.PLine,
            release.CLine,
            release.OriginalReleaseDate,
            release.MetadataComplete,
            coverArtUrl,
            release.PublishedAt,
            release.CreatedAt,
            release.UpdatedAt,
            collaborators,
            release.Tracks
                .OrderBy(t => t.TrackNumber)
                .Select(t => new ManageTrackResponse(
                    t.Id.Value,
                    t.Title,
                    t.TrackNumber,
                    t.Duration.Milliseconds,
                    t.ExplicitFlag,
                    t.Isrc,
                    t.Lyrics,
                    t.LanguageCode,
                    t.VersionTitle,
                    t.ComposerCredits,
                    t.LifecycleStatus,
                    HasAudioMaster: !string.IsNullOrEmpty(t.AudioMasterKey),
                    HasAudioStream: !string.IsNullOrEmpty(t.AudioStreamKey)))
                .ToArray());
}

internal sealed class CheckReleaseSlugAvailabilityHandler(CatalogDbContext db)
{
    public async Task<Result<ReleaseSlugAvailabilityResponse>> HandleAsync(
        Guid artistId,
        string rawSlug,
        Guid? excludingReleaseId,
        CancellationToken cancellationToken)
    {
        if (artistId == Guid.Empty)
            return Result<ReleaseSlugAvailabilityResponse>.Failure(CatalogErrors.ArtistNotFound);

        var typedArtistId = ArtistId.From(artistId);
        var artistExists = await db.Artists
            .AsNoTracking()
            .AnyAsync(a => a.Id == typedArtistId, cancellationToken);
        if (!artistExists)
            return Result<ReleaseSlugAvailabilityResponse>.Failure(CatalogErrors.ArtistNotFound);

        var normalized = CatalogSlugHelper.NormalizeSlugInput(rawSlug);
        var parseResult = CatalogSlugHelper.TryParseReleaseSlug(rawSlug);
        if (!parseResult.IsSuccess)
        {
            return Result<ReleaseSlugAvailabilityResponse>.Success(
                new ReleaseSlugAvailabilityResponse(normalized, IsValid: false, IsAvailable: false));
        }

        ReleaseId? typedExcluding = excludingReleaseId is { } id && id != Guid.Empty
            ? ReleaseId.From(id)
            : null;

        var available = await CatalogSlugHelper.IsReleaseSlugAvailableAsync(
            db,
            typedArtistId,
            rawSlug,
            typedExcluding,
            cancellationToken);

        return Result<ReleaseSlugAvailabilityResponse>.Success(
            new ReleaseSlugAvailabilityResponse(
                parseResult.Value!.Value,
                IsValid: true,
                IsAvailable: available));
    }
}

internal static class ReleaseGroupLookup
{
    internal readonly record struct Display(string? Title, string? Slug);

    internal static async Task<Display> LoadDisplayAsync(
        CatalogDbContext db,
        ReleaseGroupId? releaseGroupId,
        CancellationToken cancellationToken)
    {
        if (releaseGroupId is null)
            return new Display(null, null);

        var group = await db.ReleaseGroups
            .AsNoTracking()
            .Where(g => g.Id == releaseGroupId)
            .Select(g => new { g.Title, Slug = g.Slug.Value })
            .FirstOrDefaultAsync(cancellationToken);

        return group is null
            ? new Display(null, null)
            : new Display(group.Title, group.Slug);
    }
}
