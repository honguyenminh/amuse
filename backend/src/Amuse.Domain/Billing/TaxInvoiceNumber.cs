namespace Amuse.Domain.Billing;

public static class TaxInvoiceNumber
{
    public static string Format(int year, int sequence) =>
        $"AM-{year:D4}-{sequence:D6}";

    public static string NextFromLatest(string? latestInvoiceNumber, DateTimeOffset issuedAt)
    {
        var year = issuedAt.Year;
        var nextSequence = 1;

        if (!string.IsNullOrWhiteSpace(latestInvoiceNumber))
        {
            var parts = latestInvoiceNumber.Trim().Split('-');
            if (parts.Length == 3
                && int.TryParse(parts[1], out var invoiceYear)
                && int.TryParse(parts[2], out var invoiceSequence)
                && invoiceYear == year)
            {
                nextSequence = invoiceSequence + 1;
            }
        }

        return Format(year, nextSequence);
    }
}
