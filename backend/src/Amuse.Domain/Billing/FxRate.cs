namespace Amuse.Domain.Billing;

public sealed class FxRate
{
    public FxRateId Id { get; private set; }
    public string BaseCurrency { get; private set; } = null!;
    public string QuoteCurrency { get; private set; } = null!;
    public decimal Rate { get; private set; }
    public FxRateSource Source { get; private set; }
    public DateTimeOffset EffectiveAt { get; private set; }
    public DateTimeOffset ImportedAt { get; private set; }

    private FxRate()
    {
    }

    public static FxRate Create(
        string baseCurrency,
        string quoteCurrency,
        decimal rate,
        FxRateSource source,
        DateTimeOffset effectiveAt,
        DateTimeOffset importedAt)
    {
        if (rate <= 0)
            throw new ArgumentOutOfRangeException(nameof(rate));

        return new FxRate
        {
            Id = FxRateId.New(),
            BaseCurrency = baseCurrency.Trim().ToUpperInvariant(),
            QuoteCurrency = quoteCurrency.Trim().ToUpperInvariant(),
            Rate = rate,
            Source = source,
            EffectiveAt = effectiveAt,
            ImportedAt = importedAt,
        };
    }
}
