using Microsoft.Extensions.Logging;

namespace Amuse.Modules.Billing.Services;

internal sealed partial class FxRateImportWorker
{
    [LoggerMessage(Level = LogLevel.Error, Message = "FX rate import worker iteration failed")]
    private partial void LogIterationFailed(Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Imported {Count} ECB FX rates")]
    private partial void LogImportedFxRates(int count);
}
