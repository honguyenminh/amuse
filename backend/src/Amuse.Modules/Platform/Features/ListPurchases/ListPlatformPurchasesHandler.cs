using Amuse.Domain.Billing;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Billing.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Platform.Features.ListPurchases;

internal sealed class ListPlatformPurchasesHandler(BillingDbContext billingDb)
{
    private const int DefaultLimit = 50;
    private const int MaxLimit = 200;

    public async Task<Result<IReadOnlyList<PlatformPurchaseRow>>> HandleAsync(
        string? query,
        string? paymentStatus,
        int? limit,
        CancellationToken cancellationToken)
    {
        var resolvedLimit = limit is null or < 1
            ? DefaultLimit
            : Math.Min(limit.Value, MaxLimit);

        IQueryable<Purchase> purchases = billingDb.Purchases.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(paymentStatus)
            && Enum.TryParse<PaymentStatus>(paymentStatus, ignoreCase: true, out var status))
        {
            purchases = purchases.Where(purchase => purchase.PaymentStatus == status);
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            var trimmed = query.Trim();
            if (Guid.TryParse(trimmed, out var purchaseId))
            {
                purchases = purchases.Where(purchase => purchase.Id == PurchaseId.From(purchaseId));
            }
            else
            {
                purchases = purchases.Where(purchase =>
                    EF.Functions.ILike(purchase.Id.Value.ToString(), $"%{trimmed}%"));
            }
        }

        var rows = await purchases
            .OrderByDescending(purchase => purchase.PurchasedAt)
            .Take(resolvedLimit)
            .Select(purchase => new PlatformPurchaseRow(
                purchase.Id.Value,
                purchase.AccountId.Value,
                purchase.OrganizationId.Value,
                purchase.PurchasedUnit.ToString().ToLowerInvariant(),
                purchase.TrackId,
                purchase.ReleaseId,
                purchase.PriceSnapshotMinor,
                purchase.Currency,
                purchase.PaymentStatus.ToString().ToLowerInvariant(),
                purchase.EntitlementStatus.ToString().ToLowerInvariant(),
                purchase.PurchasedAt,
                purchase.PaidAt,
                purchase.RefundReason,
                purchase.RefundFeeBearer.HasValue
                    ? purchase.RefundFeeBearer.Value.ToString().ToLowerInvariant()
                    : null,
                purchase.RefundedAt))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<PlatformPurchaseRow>>.Success(rows);
    }
}

public sealed record PlatformPurchaseRow(
    Guid Id,
    Guid BuyerAccountId,
    Guid ListingOrganizationId,
    string PurchasedUnit,
    Guid? TrackId,
    Guid? ReleaseId,
    long PriceSnapshotMinor,
    string Currency,
    string PaymentStatus,
    string EntitlementStatus,
    DateTimeOffset PurchasedAt,
    DateTimeOffset? PaidAt,
    string? RefundReason,
    string? RefundFeeBearer,
    DateTimeOffset? RefundedAt);
