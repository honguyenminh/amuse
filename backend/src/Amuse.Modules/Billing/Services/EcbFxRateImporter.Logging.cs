using Microsoft.Extensions.Logging;

namespace Amuse.Modules.Billing.Services;

internal sealed partial class EcbFxRateImporter
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "ECB daily feed missing USD rate; skipping import")]
    private partial void LogUsdRateMissing();

    [LoggerMessage(Level = LogLevel.Debug, Message = "ECB daily feed missing {Currency}; skipping pair")]
    private partial void LogCurrencyRateMissing(string currency);
}
