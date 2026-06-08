namespace Amuse.Domain.Billing;

public sealed class PlatformFeeConfig
{
    public int DefaultRateBps { get; init; } = 1000;

    public DateTimeOffset? EffectiveFrom { get; init; }
}

public sealed class TaxConfig
{
    public int DefaultVatBps { get; init; } = 1000;

    public bool PricesTaxInclusive { get; init; } = true;
}

public sealed class WithdrawalAutoApproveConfig
{
    public long MaxAutoApproveUsdMinor { get; init; } = 500_000;

    public int CooldownDays { get; init; } = 7;
}

public sealed class HoldConfig
{
    public int Days { get; init; } = JournalPoster.DefaultHoldDays;
}

public sealed class StripeConfig
{
    public const string SectionName = "Billing:Stripe";

    public string SecretKey { get; init; } = string.Empty;

    public string WebhookSecret { get; init; } = string.Empty;

    public string PublishableKey { get; init; } = string.Empty;
}

public sealed class CheckoutConfig
{
    public const string SectionName = "Billing:Checkout";

    public string SuccessUrl { get; init; } = string.Empty;

    public string CancelUrl { get; init; } = string.Empty;
}

public sealed class GlobalPayoutConfig
{
    public const string SectionName = "Billing:GlobalPayout";

    public string AccountLinkReturnUrl { get; init; } = string.Empty;

    public string AccountLinkRefreshUrl { get; init; } = string.Empty;
}

public sealed class FxRateImportConfig
{
    public const string SectionName = "Billing:FxRateImport";

    public string EcbDailyUrl { get; init; } =
        "https://www.ecb.europa.eu/stats/eurofxref/eurofxref-daily.xml";

    public int RunIntervalHours { get; init; } = 24;

    public IReadOnlyList<string> SupportedQuoteCurrencies { get; init; } = ["VND", "EUR", "GBP"];
}
