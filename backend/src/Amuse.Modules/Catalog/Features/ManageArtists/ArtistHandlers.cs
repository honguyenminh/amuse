using System.Security.Claims;
using Amuse.Domain.Catalog;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Catalog.Features.Shared;
using Amuse.Modules.Catalog.Persistence;
using Amuse.Modules.Common.Time;
using Amuse.Modules.Media;
using Amuse.Modules.Tenancy.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Catalog.Features.ManageArtists;

internal sealed class CreateArtistHandler(
    CatalogDbContext db,
    ITenancyOrganizationReadModel organizationReadModel,
    IClock clock,
    CatalogAuditWriter auditWriter)
{
    public async Task<Result<ManageArtistSummaryResponse>> HandleAsync(
        CreateArtistRequest request,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var orgResult = CatalogPersonaAccessor.GetOrganizationId(principal);
        if (!orgResult.IsSuccess)
            return Result<ManageArtistSummaryResponse>.Failure(orgResult.Error!);

        var organizationId = orgResult.Value!;
        var now = clock.UtcNow;

        var trustTier = await organizationReadModel.GetTrustTierAsync(organizationId, cancellationToken);
        if (trustTier is null)
            return Result<ManageArtistSummaryResponse>.Failure(CatalogErrors.Forbidden);

        var slugResult = await CatalogSlugHelper.EnsureAvailableArtistSlugAsync(
            db,
            request.Slug,
            cancellationToken);
        if (!slugResult.IsSuccess)
            return Result<ManageArtistSummaryResponse>.Failure(slugResult.Error!);

        var visibilityTier = ArtistVisibilityTierMapper.FromOrganizationTrustTier(trustTier.Value);

        var createResult = Artist.Create(
            ArtistId.New(),
            request.Name,
            slugResult.Value!,
            now,
            managingOrganizationId: organizationId,
            visibilityTier,
            bio: request.Bio,
            countryCode: request.CountryCode,
            websiteUrl: request.WebsiteUrl,
            aliases: request.Aliases);

        if (!createResult.IsSuccess)
            return Result<ManageArtistSummaryResponse>.Failure(createResult.Error!);

        var artist = createResult.Value!;
        db.Artists.Add(artist);
        await db.SaveChangesAsync(cancellationToken);

        await auditWriter.WriteCreateAsync(
            CatalogAuditTables.Artist,
            artist.Id.Value,
            CatalogAuditSnapshotMapper.FromArtist(artist),
            CatalogAccountAccessor.TryGetAccountId(principal),
            cancellationToken);

        return Result<ManageArtistSummaryResponse>.Success(ArtistMapper.ToSummary(artist));
    }
}

internal sealed class CheckArtistSlugAvailabilityHandler(CatalogDbContext db)
{
    public async Task<Result<ArtistSlugAvailabilityResponse>> HandleAsync(
        string rawSlug,
        CancellationToken cancellationToken)
    {
        var normalized = CatalogSlugHelper.NormalizeSlugInput(rawSlug);
        var parseResult = CatalogSlugHelper.TryParseArtistSlug(rawSlug);
        if (!parseResult.IsSuccess)
        {
            return Result<ArtistSlugAvailabilityResponse>.Success(
                new ArtistSlugAvailabilityResponse(normalized, IsValid: false, IsAvailable: false));
        }

        var available = await CatalogSlugHelper.IsArtistSlugAvailableAsync(
            db,
            rawSlug,
            excludingArtistId: null,
            cancellationToken);

        return Result<ArtistSlugAvailabilityResponse>.Success(
            new ArtistSlugAvailabilityResponse(
                parseResult.Value!.Value,
                IsValid: true,
                IsAvailable: available));
    }
}

internal sealed class ListArtistsHandler(CatalogDbContext db)
{
    public async Task<Result<ManageArtistListResponse>> HandleAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var orgResult = CatalogPersonaAccessor.GetOrganizationId(principal);
        if (!orgResult.IsSuccess)
            return Result<ManageArtistListResponse>.Failure(orgResult.Error!);

        var organizationId = orgResult.Value!;

        var items = await db.Artists
            .AsNoTracking()
            .Where(a => a.ManagingOrganizationId == organizationId)
            .OrderBy(a => a.Name)
            .Select(a => new ManageArtistSummaryResponse(
                a.Id.Value,
                a.Slug.Value,
                a.Name,
                a.VisibilityTier,
                a.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result<ManageArtistListResponse>.Success(new ManageArtistListResponse(items));
    }
}

internal sealed class GetArtistHandler(CatalogDbContext db, IMediaPublicUrlBuilder mediaUrls)
{
    public async Task<Result<ManageArtistDetailResponse>> HandleAsync(
        Guid artistId,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var orgResult = CatalogPersonaAccessor.GetOrganizationId(principal);
        if (!orgResult.IsSuccess)
            return Result<ManageArtistDetailResponse>.Failure(orgResult.Error!);

        if (artistId == Guid.Empty)
            return Result<ManageArtistDetailResponse>.Failure(CatalogErrors.ArtistNotFound);

        var typedId = ArtistId.From(artistId);
        var artist = await db.Artists
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == typedId, cancellationToken);

        if (artist is null)
            return Result<ManageArtistDetailResponse>.Failure(CatalogErrors.ArtistNotFound);

        var scopeResult = CatalogScopeGuard.EnsureArtistManagedBy(artist, orgResult.Value!);
        if (!scopeResult.IsSuccess)
            return Result<ManageArtistDetailResponse>.Failure(scopeResult.Error!);

        var releaseRows = await db.Releases
            .AsNoTracking()
            .Where(r => r.ArtistId == typedId)
            .OrderByDescending(r => r.ReleaseDate)
            .Select(r => new
            {
                r.Id,
                r.Slug,
                r.Title,
                r.ReleaseType,
                r.LifecycleStatus,
                r.ReleaseDate,
                r.CoverArtKey,
            })
            .ToListAsync(cancellationToken);

        var releases = releaseRows
            .Select(r => new ManageArtistReleaseSummary(
                r.Id.Value,
                r.Slug.Value,
                r.Title,
                r.ReleaseType,
                r.LifecycleStatus,
                r.ReleaseDate,
                mediaUrls.BuildCoverArtUrl(r.CoverArtKey)))
            .ToArray();

        var tracks = await db.Tracks
            .AsNoTracking()
            .Where(t => t.OrganizationId == orgResult.Value! && db.Releases.Any(r => r.Id == t.ReleaseId && r.ArtistId == typedId))
            .OrderBy(t => t.TrackNumber)
            .Select(t => new ManageArtistTrackSummary(
                t.Id.Value,
                t.Title,
                t.TrackNumber,
                t.Duration.Milliseconds,
                t.ExplicitFlag,
                t.LifecycleStatus))
            .ToListAsync(cancellationToken);

        var releaseGroups = await db.ReleaseGroups
            .AsNoTracking()
            .Where(g => g.ArtistId == typedId)
            .OrderBy(g => g.Title)
            .Select(g => new ManageReleaseGroupSummaryResponse(
                g.Id.Value,
                g.Slug.Value,
                g.Title,
                g.Description,
                db.Releases.Count(r => r.ReleaseGroupId == g.Id),
                g.UpdatedAt))
            .ToListAsync(cancellationToken);

        return Result<ManageArtistDetailResponse>.Success(
            new ManageArtistDetailResponse(
                artist.Id.Value,
                artist.Slug.Value,
                artist.Name,
                artist.Bio,
                artist.CountryCode,
                artist.WebsiteUrl,
                artist.Aliases,
                mediaUrls.BuildCoverArtUrl(artist.AvatarKey),
                mediaUrls.BuildCoverArtUrl(artist.CoverKey),
                artist.VisibilityTier,
                artist.CreatedAt,
                releases,
                tracks,
                releaseGroups));
    }
}

internal sealed class UpdateArtistHandler(CatalogDbContext db, CatalogAuditWriter auditWriter)
{
    public async Task<Result<ManageArtistSummaryResponse>> HandleAsync(
        Guid artistId,
        UpdateArtistRequest request,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var orgResult = CatalogPersonaAccessor.GetOrganizationId(principal);
        if (!orgResult.IsSuccess)
            return Result<ManageArtistSummaryResponse>.Failure(orgResult.Error!);

        if (artistId == Guid.Empty)
            return Result<ManageArtistSummaryResponse>.Failure(CatalogErrors.ArtistNotFound);

        var typedId = ArtistId.From(artistId);
        var artist = await db.Artists
            .FirstOrDefaultAsync(a => a.Id == typedId, cancellationToken);

        if (artist is null)
            return Result<ManageArtistSummaryResponse>.Failure(CatalogErrors.ArtistNotFound);

        var scopeResult = CatalogScopeGuard.EnsureArtistManagedBy(artist, orgResult.Value!);
        if (!scopeResult.IsSuccess)
            return Result<ManageArtistSummaryResponse>.Failure(scopeResult.Error!);

        var before = CatalogAuditSnapshotMapper.FromArtist(artist);
        var updateResult = artist.UpdateProfile(
            request.Name,
            request.Bio,
            request.CountryCode,
            request.WebsiteUrl,
            request.Aliases);
        if (!updateResult.IsSuccess)
            return Result<ManageArtistSummaryResponse>.Failure(updateResult.Error!);

        await db.SaveChangesAsync(cancellationToken);

        await auditWriter.WriteUpdateAsync(
            CatalogAuditTables.Artist,
            artist.Id.Value,
            before,
            CatalogAuditSnapshotMapper.FromArtist(artist),
            CatalogAccountAccessor.TryGetAccountId(principal),
            cancellationToken);

        return Result<ManageArtistSummaryResponse>.Success(ArtistMapper.ToSummary(artist));
    }
}

internal static class ArtistMapper
{
    internal static ManageArtistSummaryResponse ToSummary(Artist artist) =>
        new(
            artist.Id.Value,
            artist.Slug.Value,
            artist.Name,
            artist.VisibilityTier,
            artist.CreatedAt);
}
