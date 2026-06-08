using System.Security.Claims;
using Amuse.Domain.Catalog;
using Amuse.Domain.SharedKernel;
using Amuse.Domain.Tenancy;
using Amuse.Modules.Catalog.Features.Common;
using Amuse.Modules.Catalog.Features.ManageReleases;
using Amuse.Modules.Catalog.Features.ManageTracks;
using Amuse.Modules.Catalog.Persistence;
using Amuse.Modules.Common.Time;
using Amuse.Modules.Media;
using Amuse.Modules.Tenancy.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Catalog.Features.ManagePricing;

internal sealed class SetTrackPricingHandler(
    CatalogDbContext db,
    ITenancyOrganizationReadModel organizationReadModel,
    IClock clock,
    CatalogAuditWriter auditWriter)
{
    public async Task<Result<ManageTrackResponse>> HandleAsync(
        Guid trackId,
        SetTrackPricingRequest request,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var orgResult = CatalogPersonaAccessor.GetOrganizationId(principal);
        if (!orgResult.IsSuccess)
            return Result<ManageTrackResponse>.Failure(orgResult.Error!);

        if (trackId == Guid.Empty)
            return Result<ManageTrackResponse>.Failure(CatalogErrors.TrackNotFound);

        var typedTrackId = TrackId.From(trackId);
        var track = await db.Tracks
            .FirstOrDefaultAsync(t => t.Id == typedTrackId, cancellationToken);

        if (track is null)
            return Result<ManageTrackResponse>.Failure(CatalogErrors.TrackNotFound);

        var scopeResult = CatalogScopeGuard.EnsureOrganizationScope(orgResult.Value!, track.OrganizationId);
        if (!scopeResult.IsSuccess)
            return Result<ManageTrackResponse>.Failure(scopeResult.Error!);

        var pricingGuard = await CatalogPricingGuard.EnsurePricingChangesAllowedAsync(
            organizationReadModel,
            track.OrganizationId,
            cancellationToken);

        if (!pricingGuard.IsSuccess)
            return Result<ManageTrackResponse>.Failure(pricingGuard.Error!);

        var before = CatalogAuditSnapshotMapper.FromTrack(track);
        var pricingResult = track.SetPricing(
            request.IsForSale,
            request.PriceFloorMinor,
            request.PriceCeilingMinor,
            request.PriceCurrency);

        if (!pricingResult.IsSuccess)
            return Result<ManageTrackResponse>.Failure(pricingResult.Error!);

        await db.SaveChangesAsync(cancellationToken);

        await auditWriter.WriteUpdateAsync(
            CatalogAuditTables.Track,
            track.Id.Value,
            before,
            CatalogAuditSnapshotMapper.FromTrack(track),
            CatalogAccountAccessor.TryGetAccountId(principal),
            cancellationToken);

        var splits = await RoyaltySplitLoader.LoadForTrackAsync(db, track.Id, cancellationToken);
        return Result<ManageTrackResponse>.Success(TrackMapper.ToResponse(track, splits));
    }
}

internal sealed class SetReleasePricingHandler(
    CatalogDbContext db,
    ITenancyOrganizationReadModel organizationReadModel,
    IClock clock,
    IMediaPublicUrlBuilder mediaUrls,
    CatalogAuditWriter auditWriter)
{
    public async Task<Result<ManageReleaseDetailResponse>> HandleAsync(
        Guid releaseId,
        SetReleasePricingRequest request,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var orgResult = CatalogPersonaAccessor.GetOrganizationId(principal);
        if (!orgResult.IsSuccess)
            return Result<ManageReleaseDetailResponse>.Failure(orgResult.Error!);

        if (releaseId == Guid.Empty)
            return Result<ManageReleaseDetailResponse>.Failure(CatalogErrors.ReleaseNotFound);

        var typedReleaseId = ReleaseId.From(releaseId);
        var release = await db.Releases
            .Include(r => r.Tracks)
            .FirstOrDefaultAsync(r => r.Id == typedReleaseId, cancellationToken);

        if (release is null)
            return Result<ManageReleaseDetailResponse>.Failure(CatalogErrors.ReleaseNotFound);

        var scopeResult = CatalogScopeGuard.EnsureOrganizationScope(orgResult.Value!, release.OrganizationId);
        if (!scopeResult.IsSuccess)
            return Result<ManageReleaseDetailResponse>.Failure(scopeResult.Error!);

        var pricingGuard = await CatalogPricingGuard.EnsurePricingChangesAllowedAsync(
            organizationReadModel,
            release.OrganizationId,
            cancellationToken);

        if (!pricingGuard.IsSuccess)
            return Result<ManageReleaseDetailResponse>.Failure(pricingGuard.Error!);

        var before = CatalogAuditSnapshotMapper.FromRelease(release);
        var pricingResult = release.SetPricing(
            request.IsForSale,
            request.PriceFloorMinor,
            request.PriceCeilingMinor,
            request.PriceCurrency,
            clock.UtcNow);

        if (!pricingResult.IsSuccess)
            return Result<ManageReleaseDetailResponse>.Failure(pricingResult.Error!);

        await db.SaveChangesAsync(cancellationToken);

        await auditWriter.WriteUpdateAsync(
            CatalogAuditTables.Release,
            release.Id.Value,
            before,
            CatalogAuditSnapshotMapper.FromRelease(release),
            CatalogAccountAccessor.TryGetAccountId(principal),
            cancellationToken);

        return await PublishRelease.PublishReleaseHandler.MapDetailAsync(
            db,
            mediaUrls,
            release,
            cancellationToken);
    }
}

internal sealed class SetTrackRoyaltySplitsHandler(
    CatalogDbContext db,
    ITenancyOrganizationReadModel organizationReadModel,
    IClock clock,
    CatalogAuditWriter auditWriter)
{
    public async Task<Result<ManageTrackResponse>> HandleAsync(
        Guid trackId,
        SetTrackRoyaltySplitsRequest request,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var orgResult = CatalogPersonaAccessor.GetOrganizationId(principal);
        if (!orgResult.IsSuccess)
            return Result<ManageTrackResponse>.Failure(orgResult.Error!);

        if (trackId == Guid.Empty)
            return Result<ManageTrackResponse>.Failure(CatalogErrors.TrackNotFound);

        var typedTrackId = TrackId.From(trackId);
        var track = await db.Tracks
            .FirstOrDefaultAsync(t => t.Id == typedTrackId, cancellationToken);

        if (track is null)
            return Result<ManageTrackResponse>.Failure(CatalogErrors.TrackNotFound);

        var scopeResult = CatalogScopeGuard.EnsureOrganizationScope(orgResult.Value!, track.OrganizationId);
        if (!scopeResult.IsSuccess)
            return Result<ManageTrackResponse>.Failure(scopeResult.Error!);

        var pricingGuard = await CatalogPricingGuard.EnsurePricingChangesAllowedAsync(
            organizationReadModel,
            track.OrganizationId,
            cancellationToken);

        if (!pricingGuard.IsSuccess)
            return Result<ManageTrackResponse>.Failure(pricingGuard.Error!);

        var entries = request.Splits
            .Select(split => new RoyaltySplitEntry(
                OrganizationId.From(split.PayeeOrganizationId),
                split.ShareBps))
            .ToArray();

        var replaceResult = RoyaltySplit.ReplaceForTrack(
            track.Id,
            track.OrganizationId,
            entries,
            clock.UtcNow);

        if (!replaceResult.IsSuccess)
            return Result<ManageTrackResponse>.Failure(replaceResult.Error!);

        var existingSplits = await db.RoyaltySplits
            .Where(split => split.TrackId == track.Id)
            .ToListAsync(cancellationToken);

        db.RoyaltySplits.RemoveRange(existingSplits);
        db.RoyaltySplits.AddRange(replaceResult.Value!);
        await db.SaveChangesAsync(cancellationToken);

        await auditWriter.WriteUpdateAsync(
            CatalogAuditTables.Track,
            track.Id.Value,
            before: null,
            after: CatalogAuditSnapshotMapper.FromTrack(track),
            CatalogAccountAccessor.TryGetAccountId(principal),
            cancellationToken);

        return Result<ManageTrackResponse>.Success(
            TrackMapper.ToResponse(track, replaceResult.Value!));
    }
}

internal static class RoyaltySplitLoader
{
    internal static async Task<IReadOnlyList<RoyaltySplit>> LoadForTrackAsync(
        CatalogDbContext db,
        TrackId trackId,
        CancellationToken cancellationToken) =>
        await db.RoyaltySplits
            .AsNoTracking()
            .Where(split => split.TrackId == trackId)
            .ToListAsync(cancellationToken);

    internal static async Task<IReadOnlyList<RoyaltySplit>> LoadForReleaseTracksAsync(
        CatalogDbContext db,
        IReadOnlyList<Track> tracks,
        CancellationToken cancellationToken)
    {
        if (tracks.Count == 0)
            return [];

        var trackIds = tracks.Select(track => track.Id).ToArray();
        return await db.RoyaltySplits
            .AsNoTracking()
            .Where(split => trackIds.Contains(split.TrackId))
            .ToListAsync(cancellationToken);
    }
}
