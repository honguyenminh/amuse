using Amuse.Domain.SharedKernel;

namespace Amuse.Domain.Billing;

public sealed record PurchaseWaterfallResult(
    long GrossMinor,
    long VatMinor,
    long NetExVatMinor,
    long PlatformFeeMinor,
    long PspFeeMinor,
    long NetToSellersMinor,
    int VatRateBps,
    int PlatformFeeRateBps,
    string Currency);

public static class PurchaseWaterfall
{
    public static Result<PurchaseWaterfallResult> Compute(
        long grossMinor,
        string currency,
        long pspFeeMinor,
        int vatRateBps,
        int platformFeeRateBps)
    {
        if (grossMinor <= 0)
            return Result<PurchaseWaterfallResult>.Failure(BillingErrors.InvalidCheckoutAmount);

        if (pspFeeMinor < 0)
            return Result<PurchaseWaterfallResult>.Failure(BillingErrors.InvalidCheckoutAmount);

        var vatMinor = ExtractInclusiveVat(grossMinor, vatRateBps);
        var netExVatMinor = grossMinor - vatMinor;
        var platformFeeMinor = RoundPercentOfGross(grossMinor, platformFeeRateBps);
        var netToSellersMinor = grossMinor - vatMinor - platformFeeMinor - pspFeeMinor;

        if (netToSellersMinor < 0)
            return Result<PurchaseWaterfallResult>.Failure(BillingErrors.InvalidLedgerJournal);

        return Result<PurchaseWaterfallResult>.Success(new PurchaseWaterfallResult(
            grossMinor,
            vatMinor,
            netExVatMinor,
            platformFeeMinor,
            pspFeeMinor,
            netToSellersMinor,
            vatRateBps,
            platformFeeRateBps,
            currency));
    }

    public static long ExtractInclusiveVat(long grossMinor, int vatRateBps)
    {
        if (vatRateBps <= 0)
            return 0;

        return (long)Math.Round(
            (double)grossMinor * vatRateBps / (10_000 + vatRateBps),
            MidpointRounding.AwayFromZero);
    }

    public static long RoundPercentOfGross(long grossMinor, int rateBps) =>
        (long)Math.Round((double)grossMinor * rateBps / 10_000, MidpointRounding.AwayFromZero);
}
