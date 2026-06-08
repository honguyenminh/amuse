using System.Text.RegularExpressions;

namespace Amuse.Domain.SharedKernel;

public readonly record struct Money
{
    public const int Iso4217CurrencyLength = 3;

    private static readonly Regex CurrencyPattern = new("^[A-Z]{3}$", RegexOptions.CultureInvariant);

    public long AmountMinor { get; }
    public string Currency { get; }

    private Money(long amountMinor, string currency)
    {
        AmountMinor = amountMinor;
        Currency = currency;
    }

    public bool IsZero => AmountMinor == 0;

    public static Result<Money> Create(long amountMinor, string currency)
    {
        if (amountMinor < 0)
            return Result<Money>.Failure(MoneyErrors.InvalidAmount);

        var normalizedCurrency = NormalizeCurrency(currency);
        if (normalizedCurrency is null)
            return Result<Money>.Failure(MoneyErrors.InvalidCurrency);

        return Result<Money>.Success(new Money(amountMinor, normalizedCurrency));
    }

    public static Result<Money> CreateNonNegative(long amountMinor, string currency) =>
        Create(amountMinor, currency);

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(AmountMinor + other.AmountMinor, Currency);
    }

    public Money Subtract(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(AmountMinor - other.AmountMinor, Currency);
    }

    public static bool IsValidCurrency(string? currency) =>
        NormalizeCurrency(currency) is not null;

    private static string? NormalizeCurrency(string? currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
            return null;

        var normalized = currency.Trim().ToUpperInvariant();
        return CurrencyPattern.IsMatch(normalized) ? normalized : null;
    }

    private void EnsureSameCurrency(Money other)
    {
        if (!string.Equals(Currency, other.Currency, StringComparison.Ordinal))
            throw new InvalidOperationException("Money operations require the same currency.");
    }
}
