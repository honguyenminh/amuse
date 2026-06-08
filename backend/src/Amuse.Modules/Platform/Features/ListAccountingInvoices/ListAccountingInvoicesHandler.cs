using Amuse.Domain.Billing;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Billing.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Platform.Features.ListAccountingInvoices;

internal sealed class ListAccountingInvoicesHandler(BillingDbContext billingDb)
{
    public async Task<Result<IReadOnlyList<PlatformTaxInvoiceRow>>> HandleAsync(
        CancellationToken cancellationToken)
    {
        var rows = await billingDb.TaxInvoices.AsNoTracking()
            .OrderByDescending(i => i.IssuedAt)
            .Take(500)
            .Select(i => new PlatformTaxInvoiceRow(
                i.Id.Value,
                i.InvoiceNumber,
                i.PurchaseId.Value,
                i.BuyerAccountId.Value,
                i.GrossMinor,
                i.VatMinor,
                i.NetExVatMinor,
                i.Currency,
                i.VatRateBps,
                i.IssuedAt))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<PlatformTaxInvoiceRow>>.Success(rows);
    }
}

public sealed record PlatformTaxInvoiceRow(
    Guid Id,
    string InvoiceNumber,
    Guid PurchaseId,
    Guid BuyerAccountId,
    long GrossMinor,
    long VatMinor,
    long NetExVatMinor,
    string Currency,
    int VatRateBps,
    DateTimeOffset IssuedAt);
