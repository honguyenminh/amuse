using Amuse.Domain.Billing;
using Amuse.Domain.Tenancy;
using Amuse.Modules.Billing.Contracts;
using Amuse.Modules.Billing.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Billing.Services;

internal sealed class LedgerBalanceReadModel(
    BillingDbContext billingDb,
    IFxRateReadModel fxRates) : ILedgerBalanceReadModel
{
    public async Task<OrgBalanceSnapshot> GetBalanceAsync(
        OrganizationId organizationId,
        CancellationToken cancellationToken)
    {
        var orgIdValue = organizationId.Value;

        var entries = await billingDb.LedgerEntries.AsNoTracking()
            .Where(entry => entry.OrganizationId == orgIdValue)
            .ToListAsync(cancellationToken);

        var receivableRows = await billingDb.SellerReceivables.AsNoTracking()
            .Where(receivable =>
                receivable.OrganizationId == organizationId
                && !receivable.IsSettled)
            .GroupBy(receivable => receivable.Currency)
            .Select(group => new { Currency = group.Key, AmountMinor = group.Sum(x => x.AmountMinor) })
            .ToListAsync(cancellationToken);

        var receivableByCurrency = receivableRows.ToDictionary(
            row => row.Currency,
            row => row.AmountMinor,
            StringComparer.Ordinal);

        var balances = SellerLedgerBalance.Aggregate(entries, orgIdValue, receivableByCurrency);
        var withUsd = new List<CurrencyBalance>(balances.Count);
        foreach (var balance in balances)
        {
            long? usdEquivalent = null;
            var totalMinor = balance.PendingMinor + balance.AvailableMinor + balance.InPayoutMinor;
            if (totalMinor > 0)
            {
                var fx = await fxRates.GetUsdEquivalentAsync(balance.Currency, totalMinor, cancellationToken);
                if (fx.IsSuccess)
                    usdEquivalent = fx.Value!.UsdEquivalentMinor;
            }

            withUsd.Add(balance with { UsdEquivalentMinor = usdEquivalent });
        }

        return new OrgBalanceSnapshot(withUsd);
    }
}
