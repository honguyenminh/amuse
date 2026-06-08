using Amuse.Domain.SharedKernel;

namespace Amuse.Domain.Catalog;

public readonly record struct CatalogPricing
{
    public const int CurrencyLength = Money.Iso4217CurrencyLength;

    public bool IsForSale { get; }
    public long PriceFloorMinor { get; }
    public long? PriceCeilingMinor { get; }
    public string? PriceCurrency { get; }

    private CatalogPricing(
        bool isForSale,
        long priceFloorMinor,
        long? priceCeilingMinor,
        string? priceCurrency)
    {
        IsForSale = isForSale;
        PriceFloorMinor = priceFloorMinor;
        PriceCeilingMinor = priceCeilingMinor;
        PriceCurrency = priceCurrency;
    }

    public static Result<CatalogPricing> TryCreate(
        bool isForSale,
        long priceFloorMinor,
        long? priceCeilingMinor,
        string? priceCurrency)
    {
        if (priceFloorMinor < 0)
            return Result<CatalogPricing>.Failure(CatalogErrors.InvalidPricing);

        if (priceCeilingMinor is < 0)
            return Result<CatalogPricing>.Failure(CatalogErrors.InvalidPricing);

        if (priceCeilingMinor.HasValue && priceCeilingMinor.Value < priceFloorMinor)
            return Result<CatalogPricing>.Failure(CatalogErrors.InvalidPricingBounds);

        if (!isForSale)
        {
            return Result<CatalogPricing>.Success(
                new CatalogPricing(
                    isForSale: false,
                    priceFloorMinor,
                    priceCeilingMinor,
                    NormalizeCurrency(priceCurrency)));
        }

        if (!Money.IsValidCurrency(priceCurrency))
            return Result<CatalogPricing>.Failure(CatalogErrors.InvalidPricingCurrency);

        return Result<CatalogPricing>.Success(
            new CatalogPricing(
                isForSale: true,
                priceFloorMinor,
                priceCeilingMinor,
                NormalizeCurrency(priceCurrency)!));
    }

    public long SumFloorWith(CatalogPricing other)
    {
        EnsureSameCurrency(other);
        return PriceFloorMinor + other.PriceFloorMinor;
    }

    public long? TrySumCeilingWith(CatalogPricing other)
    {
        if (!PriceCeilingMinor.HasValue || !other.PriceCeilingMinor.HasValue)
            return null;

        EnsureSameCurrency(other);
        return PriceCeilingMinor.Value + other.PriceCeilingMinor.Value;
    }

    private void EnsureSameCurrency(CatalogPricing other)
    {
        if (!string.Equals(PriceCurrency, other.PriceCurrency, StringComparison.Ordinal))
            throw new InvalidOperationException("Pricing operations require the same currency.");
    }

    private static string? NormalizeCurrency(string? currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
            return null;

        return currency.Trim().ToUpperInvariant();
    }
}
