namespace Amuse.Domain.Billing;

public sealed record CurrencyBalance(
    string Currency,
    long PendingMinor,
    long AvailableMinor,
    long InPayoutMinor,
    long ReceivableMinor,
    long? UsdEquivalentMinor);

public static class SellerLedgerBalance
{
    public static long NetBalance(
        IEnumerable<LedgerEntry> entries,
        LedgerAccountType accountType,
        Guid organizationId,
        string currency)
    {
        return entries
            .Where(entry =>
                entry.OrganizationId == organizationId
                && entry.AccountType == accountType
                && string.Equals(entry.Currency, currency, StringComparison.Ordinal))
            .Sum(entry => entry.Direction == EntryDirection.Credit
                ? entry.AmountMinor
                : -entry.AmountMinor);
    }

    public static IReadOnlyList<CurrencyBalance> Aggregate(
        IEnumerable<LedgerEntry> entries,
        Guid organizationId,
        IReadOnlyDictionary<string, long> receivableByCurrency,
        Func<string, long, long>? usdEquivalentCalculator = null)
    {
        var currencies = entries
            .Where(entry => entry.OrganizationId == organizationId)
            .Select(entry => entry.Currency)
            .Concat(receivableByCurrency.Keys)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(currency => currency, StringComparer.Ordinal)
            .ToArray();

        var balances = new List<CurrencyBalance>(currencies.Length);
        foreach (var currency in currencies)
        {
            var pending = NetBalance(entries, LedgerAccountType.SellerPayablePending, organizationId, currency);
            var available = NetBalance(entries, LedgerAccountType.SellerPayableAvailable, organizationId, currency);
            var inPayout = NetBalance(entries, LedgerAccountType.SellerPayableInPayout, organizationId, currency);
            receivableByCurrency.TryGetValue(currency, out var receivable);

            if (pending == 0 && available == 0 && inPayout == 0 && receivable == 0)
                continue;

            long? usdEquivalent = null;
            if (usdEquivalentCalculator is not null)
            {
                var totalMinor = pending + available + inPayout;
                if (totalMinor > 0)
                    usdEquivalent = usdEquivalentCalculator(currency, totalMinor);
            }

            balances.Add(new CurrencyBalance(
                currency,
                pending,
                available,
                inPayout,
                receivable,
                usdEquivalent));
        }

        return balances;
    }
}
