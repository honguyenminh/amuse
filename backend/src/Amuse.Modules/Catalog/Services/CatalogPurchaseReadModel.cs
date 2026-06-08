using Amuse.Domain.Catalog;
using Amuse.Modules.Catalog.Contracts;
using Amuse.Modules.Catalog.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Catalog.Services;

internal sealed class CatalogPurchaseReadModel(CatalogDbContext db) : ICatalogPurchaseReadModel
{
    public async Task<CatalogSellableTrackRow?> GetSellableTrackAsync(
        TrackId trackId,
        CancellationToken cancellationToken)
    {
        return await db.Tracks.AsNoTracking()
            .Where(t =>
                t.Id == trackId
                && t.LifecycleStatus == TrackLifecycleStatus.Published
                && t.IsForSale)
            .Select(t => new CatalogSellableTrackRow(
                t.Id.Value,
                t.ReleaseId.Value,
                t.OrganizationId,
                t.PriceFloorMinor,
                t.PriceCeilingMinor,
                t.PriceCurrency,
                t.IsForSale))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<CatalogSellableReleaseRow?> GetSellableReleaseAsync(
        ReleaseId releaseId,
        CancellationToken cancellationToken)
    {
        return await db.Releases.AsNoTracking()
            .Where(r =>
                r.Id == releaseId
                && r.LifecycleStatus == ReleaseLifecycleStatus.Published
                && r.IsForSale)
            .Select(r => new CatalogSellableReleaseRow(
                r.Id.Value,
                r.OrganizationId,
                r.PriceFloorMinor,
                r.PriceCeilingMinor,
                r.PriceCurrency,
                r.IsForSale))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> GetSellablePublishedTrackIdsForReleaseAsync(
        ReleaseId releaseId,
        CancellationToken cancellationToken) =>
        await db.Tracks.AsNoTracking()
            .Where(t =>
                t.ReleaseId == releaseId
                && t.LifecycleStatus == TrackLifecycleStatus.Published
                && t.IsForSale)
            .OrderBy(t => t.TrackNumber)
            .Select(t => t.Id.Value)
            .ToListAsync(cancellationToken);

    public async Task<CatalogCheckoutTrackRow?> GetCheckoutTrackAsync(
        TrackId trackId,
        CancellationToken cancellationToken)
    {
        var track = await db.Tracks.AsNoTracking()
            .Where(t =>
                t.Id == trackId
                && t.LifecycleStatus == TrackLifecycleStatus.Published
                && t.IsForSale)
            .Select(t => new
            {
                t.Id,
                ReleaseId = t.ReleaseId.Value,
                t.OrganizationId,
                t.Title,
                t.PriceFloorMinor,
                t.PriceCeilingMinor,
                t.PriceCurrency,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (track is null)
            return null;

        var splits = await LoadRoyaltySplitsForTrackAsync(track.Id, cancellationToken);

        return new CatalogCheckoutTrackRow(
            track.Id.Value,
            track.ReleaseId,
            track.OrganizationId,
            track.Title,
            track.PriceFloorMinor,
            track.PriceCeilingMinor,
            NormalizeCurrency(track.PriceCurrency),
            splits);
    }

    public async Task<CatalogCheckoutReleaseRow?> GetCheckoutReleaseAsync(
        ReleaseId releaseId,
        CancellationToken cancellationToken)
    {
        var release = await db.Releases.AsNoTracking()
            .Where(r =>
                r.Id == releaseId
                && r.LifecycleStatus == ReleaseLifecycleStatus.Published
                && r.IsForSale)
            .Select(r => new
            {
                r.Id,
                r.OrganizationId,
                r.Title,
                r.PriceFloorMinor,
                r.PriceCeilingMinor,
                r.PriceCurrency,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (release is null)
            return null;

        var tracks = await db.Tracks.AsNoTracking()
            .Where(t =>
                t.ReleaseId == releaseId
                && t.LifecycleStatus == TrackLifecycleStatus.Published
                && t.IsForSale)
            .OrderBy(t => t.TrackNumber)
            .Select(t => new
            {
                TrackId = t.Id,
                t.OrganizationId,
                t.PriceFloorMinor,
            })
            .ToListAsync(cancellationToken);

        if (tracks.Count == 0)
            return null;

        var trackIds = tracks.Select(t => t.TrackId).ToArray();
        var allSplits = await db.RoyaltySplits.AsNoTracking()
            .Where(s => trackIds.Contains(s.TrackId))
            .Select(s => new CatalogRoyaltySplitRow(
                s.TrackId.Value,
                s.PayeeOrganizationId,
                s.ShareBps))
            .ToListAsync(cancellationToken);

        var releaseTracks = tracks
            .Select(track => new CatalogCheckoutReleaseTrackRow(
                track.TrackId.Value,
                track.OrganizationId,
                track.PriceFloorMinor,
                allSplits.Where(s => s.TrackId == track.TrackId.Value).ToArray()))
            .ToArray();

        return new CatalogCheckoutReleaseRow(
            release.Id.Value,
            release.OrganizationId,
            release.Title,
            release.PriceFloorMinor,
            release.PriceCeilingMinor,
            NormalizeCurrency(release.PriceCurrency),
            releaseTracks);
    }

    private async Task<IReadOnlyList<CatalogRoyaltySplitRow>> LoadRoyaltySplitsForTrackAsync(
        TrackId trackId,
        CancellationToken cancellationToken) =>
        await db.RoyaltySplits.AsNoTracking()
            .Where(s => s.TrackId == trackId)
            .Select(s => new CatalogRoyaltySplitRow(
                s.TrackId.Value,
                s.PayeeOrganizationId,
                s.ShareBps))
            .ToListAsync(cancellationToken);

    private static string NormalizeCurrency(string? currency) =>
        string.IsNullOrWhiteSpace(currency) ? "USD" : currency.Trim().ToUpperInvariant();
}
