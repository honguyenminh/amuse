namespace Amuse.Domain.Billing;

public static class CreditNoteNumber
{
    public static string Format(int year, int sequence) =>
        $"CN-{year:D4}-{sequence:D6}";

    public static string NextFromLatest(string? latestCreditNoteNumber, DateTimeOffset issuedAt)
    {
        var year = issuedAt.Year;
        var nextSequence = 1;

        if (!string.IsNullOrWhiteSpace(latestCreditNoteNumber))
        {
            var parts = latestCreditNoteNumber.Trim().Split('-');
            if (parts.Length == 3
                && int.TryParse(parts[1], out var noteYear)
                && int.TryParse(parts[2], out var noteSequence)
                && noteYear == year)
            {
                nextSequence = noteSequence + 1;
            }
        }

        return Format(year, nextSequence);
    }
}
