using System.Security.Claims;
using Amuse.Domain.Billing;
using Amuse.Domain.Catalog;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Billing.Features.Common;
using Amuse.Modules.Billing.Persistence;
using Amuse.Modules.Catalog.Contracts;
using Amuse.Modules.Discovery.Features.Common;
using Amuse.Modules.Identity.Contracts;
using Amuse.Modules.Media;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Billing.Features.ListMyPurchases;

internal sealed class ListMyPurchasesHandler(
    BillingDbContext billingDb,
    ICatalogDiscoveryReadModel catalogDiscovery,
    IListenerPersonaReadModel personaReadModel,
    IMediaPublicUrlBuilder mediaUrls)
{
    public async Task<Result<MyPurchasesResponse>> HandleAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var listenerResult = await DiscoveryPrincipal.RequireListenerAsync(
            principal, personaReadModel, cancellationToken);
        if (!listenerResult.IsSuccess)
            return Result<MyPurchasesResponse>.Failure(listenerResult.Error!);

        var accountId = listenerResult.Value!.AccountId;

        var purchases = await billingDb.Purchases.AsNoTracking()
            .Where(p => p.AccountId == accountId && p.EntitlementStatus == EntitlementStatus.Active)
            .OrderByDescending(p => p.PurchasedAt)
            .ToListAsync(cancellationToken);

        if (purchases.Count == 0)
            return Result<MyPurchasesResponse>.Success(new MyPurchasesResponse([], []));

        var trackPurchases = purchases
            .Where(p => p.PurchasedUnit == PurchasedUnit.Track && p.TrackId.HasValue)
            .ToList();

        var releasePurchases = purchases
            .Where(p => p.PurchasedUnit == PurchasedUnit.Release && p.ReleaseId.HasValue)
            .ToList();

        var trackIds = trackPurchases.Select(p => p.TrackId!.Value).Distinct().ToArray();
        var trackRows = trackIds.Length == 0
            ? new Dictionary<Guid, CatalogTrackPlayableRow>()
            : await catalogDiscovery.GetPlayableTrackRowsAsync(trackIds, cancellationToken);

        var releaseIds = purchases
            .Select(p => p.PurchasedUnit == PurchasedUnit.Release ? p.ReleaseId : null)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Concat(trackRows.Values.Select(r => r.ReleaseId))
            .Distinct()
            .ToArray();

        var releaseSummaries = releaseIds.Length == 0
            ? new Dictionary<Guid, CatalogReleaseSummaryRow>()
            : await catalogDiscovery.GetReleaseSummariesAsync(releaseIds, cancellationToken);

        var tracks = trackPurchases
            .Where(p => trackRows.ContainsKey(p.TrackId!.Value))
            .Select(p =>
            {
                var row = trackRows[p.TrackId!.Value];
                releaseSummaries.TryGetValue(row.ReleaseId, out var releaseSummary);
                return new PurchasedTrackRow(
                    p.Id.Value,
                    p.TrackId!.Value,
                    row.ReleaseId,
                    row.ReleaseTitle,
                    row.Title,
                    row.ArtistName,
                    row.ArtistSlug,
                    row.ReleaseSlug,
                    mediaUrls.BuildCoverArtUrl(releaseSummary?.CoverArtKey),
                    p.PriceSnapshotMinor,
                    p.Currency,
                    p.PaymentStatus.ToString().ToLowerInvariant(),
                    p.PurchasedAt);
            })
            .ToArray();

        var releases = releasePurchases
            .Where(p => releaseSummaries.ContainsKey(p.ReleaseId!.Value))
            .Select(p =>
            {
                var row = releaseSummaries[p.ReleaseId!.Value];
                return new PurchasedReleaseRow(
                    p.Id.Value,
                    p.ReleaseId!.Value,
                    row.Title,
                    row.ArtistName,
                    row.ArtistSlug,
                    row.ReleaseSlug,
                    mediaUrls.BuildCoverArtUrl(row.CoverArtKey),
                    p.PriceSnapshotMinor,
                    p.Currency,
                    p.PaymentStatus.ToString().ToLowerInvariant(),
                    p.PurchasedAt);
            })
            .ToArray();

        return Result<MyPurchasesResponse>.Success(new MyPurchasesResponse(tracks, releases));
    }
}
