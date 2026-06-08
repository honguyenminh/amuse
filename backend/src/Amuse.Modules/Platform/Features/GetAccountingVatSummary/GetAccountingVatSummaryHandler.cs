using Amuse.Domain.Billing;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Billing.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Platform.Features.GetAccountingVatSummary;

internal sealed class GetAccountingVatSummaryHandler(BillingDbContext billingDb)
{
    public async Task<Result<PlatformVatSummaryResponse>> HandleAsync(
        DateTimeOffset? from,
        DateTimeOffset? to,
        CancellationToken cancellationToken)
    {
        var fromAt = from ?? DateTimeOffset.UtcNow.AddMonths(-1);
        var toAt = to ?? DateTimeOffset.UtcNow;

        if (toAt < fromAt)
            (fromAt, toAt) = (toAt, fromAt);

        var invoiceVat = await billingDb.TaxInvoices.AsNoTracking()
            .Where(invoice => invoice.IssuedAt >= fromAt && invoice.IssuedAt <= toAt)
            .GroupBy(invoice => invoice.Currency)
            .Select(group => new CurrencyVatMovementRow(
                group.Key,
                group.Sum(invoice => invoice.VatMinor),
                0L))
            .ToListAsync(cancellationToken);

        var creditVat = await billingDb.CreditNotes.AsNoTracking()
            .Where(note => note.IssuedAt >= fromAt && note.IssuedAt <= toAt)
            .GroupBy(note => note.Currency)
            .Select(group => new { group.Key, VatMinor = group.Sum(note => note.VatMinor) })
            .ToListAsync(cancellationToken);

        var creditedByCurrency = creditVat.ToDictionary(row => row.Key, row => row.VatMinor, StringComparer.Ordinal);
        var currencies = invoiceVat.Select(row => row.Currency)
            .Concat(creditedByCurrency.Keys)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(currency => currency, StringComparer.Ordinal)
            .ToArray();

        var rows = currencies
            .Select(currency =>
            {
                var invoiced = invoiceVat.FirstOrDefault(row => row.Currency == currency)?.InvoicedVatMinor ?? 0;
                creditedByCurrency.TryGetValue(currency, out var credited);
                return new CurrencyVatMovementRow(currency, invoiced, credited);
            })
            .Where(row => row.InvoicedVatMinor > 0 || row.CreditedVatMinor > 0)
            .ToArray();

        var ledgerMovements = await billingDb.LedgerEntries.AsNoTracking()
            .Where(entry => entry.AccountType == LedgerAccountType.VatPayable)
            .Join(
                billingDb.LedgerJournals.AsNoTracking(),
                entry => entry.JournalId,
                journal => journal.Id,
                (entry, journal) => new { entry, journal })
            .Where(pair => pair.journal.PostedAt >= fromAt && pair.journal.PostedAt <= toAt)
            .GroupBy(pair => pair.entry.Currency)
            .Select(group => new CurrencyLedgerVatRow(
                group.Key,
                group.Where(pair => pair.entry.Direction == EntryDirection.Credit)
                    .Sum(pair => pair.entry.AmountMinor),
                group.Where(pair => pair.entry.Direction == EntryDirection.Debit)
                    .Sum(pair => pair.entry.AmountMinor)))
            .ToListAsync(cancellationToken);

        return Result<PlatformVatSummaryResponse>.Success(new PlatformVatSummaryResponse(
            fromAt,
            toAt,
            rows,
            ledgerMovements));
    }
}

public sealed record CurrencyVatMovementRow(
    string Currency,
    long InvoicedVatMinor,
    long CreditedVatMinor)
{
    public long NetVatMinor => InvoicedVatMinor - CreditedVatMinor;
}

public sealed record CurrencyLedgerVatRow(
    string Currency,
    long CreditedMinor,
    long DebitedMinor)
{
    public long NetMovementMinor => CreditedMinor - DebitedMinor;
}

public sealed record PlatformVatSummaryResponse(
    DateTimeOffset From,
    DateTimeOffset To,
    IReadOnlyList<CurrencyVatMovementRow> InvoiceMovements,
    IReadOnlyList<CurrencyLedgerVatRow> LedgerMovements);
