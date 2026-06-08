using Amuse.Domain.Tenancy;

namespace Amuse.Domain.Billing;

public sealed record ClawbackLedgerLine(
    OrganizationId OrganizationId,
    LedgerAccountType AccountType,
    long AmountMinor);

public sealed record ClawbackReceivableLine(
    OrganizationId OrganizationId,
    long AmountMinor);

public sealed record RefundClawbackResult(
    IReadOnlyList<ClawbackLedgerLine> LedgerLines,
    IReadOnlyList<ClawbackReceivableLine> Receivables);

public static class RefundClawback
{
    public static RefundClawbackResult Compute(
        IReadOnlyList<PurchaseAllocationSnapshot> snapshots,
        IReadOnlyList<LedgerEntry> ledgerEntries,
        long refundFeeMinor,
        RefundFeeBearer refundFeeBearer,
        string currency)
    {
        var owedByOrg = snapshots
            .GroupBy(snapshot => snapshot.PayeeOrganizationId)
            .ToDictionary(
                group => group.Key,
                group => group.Sum(line => line.AmountMinor));

        if (refundFeeMinor > 0 && refundFeeBearer == RefundFeeBearer.Seller)
        {
            DistributeProRata(owedByOrg, refundFeeMinor);
        }

        var ledgerLines = new List<ClawbackLedgerLine>();
        var receivables = new List<ClawbackReceivableLine>();

        foreach (var (organizationId, totalOwed) in owedByOrg.Where(pair => pair.Value > 0))
        {
            var pendingBalance = SellerLedgerBalance.NetBalance(
                ledgerEntries,
                LedgerAccountType.SellerPayablePending,
                organizationId.Value,
                currency);

            var fromPending = Math.Min(totalOwed, Math.Max(0, pendingBalance));
            if (fromPending > 0)
            {
                ledgerLines.Add(new ClawbackLedgerLine(
                    organizationId,
                    LedgerAccountType.SellerPayablePending,
                    fromPending));
            }

            var remaining = totalOwed - fromPending;
            if (remaining <= 0)
                continue;

            var availableBalance = SellerLedgerBalance.NetBalance(
                ledgerEntries,
                LedgerAccountType.SellerPayableAvailable,
                organizationId.Value,
                currency);

            var fromAvailable = Math.Min(remaining, Math.Max(0, availableBalance));
            if (fromAvailable > 0)
            {
                ledgerLines.Add(new ClawbackLedgerLine(
                    organizationId,
                    LedgerAccountType.SellerPayableAvailable,
                    fromAvailable));
            }

            var receivableAmount = remaining - fromAvailable;
            if (receivableAmount > 0)
            {
                receivables.Add(new ClawbackReceivableLine(organizationId, receivableAmount));
            }
        }

        return new RefundClawbackResult(ledgerLines, receivables);
    }

    private static void DistributeProRata(
        Dictionary<OrganizationId, long> owedByOrg,
        long refundFeeMinor)
    {
        var totalBase = owedByOrg.Values.Sum(amount => amount);
        if (totalBase <= 0)
            return;

        var orgs = owedByOrg.Keys.ToArray();
        var allocations = new long[orgs.Length];
        var allocated = 0L;

        for (var index = 0; index < orgs.Length; index++)
        {
            if (index == orgs.Length - 1)
            {
                allocations[index] = refundFeeMinor - allocated;
                continue;
            }

            var share = (long)Math.Round(
                (double)refundFeeMinor * owedByOrg[orgs[index]] / totalBase,
                MidpointRounding.AwayFromZero);
            allocations[index] = share;
            allocated += share;
        }

        for (var index = 0; index < orgs.Length; index++)
            owedByOrg[orgs[index]] += allocations[index];
    }
}
