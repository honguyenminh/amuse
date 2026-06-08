using Amuse.Domain.Billing;
using Amuse.Domain.SharedKernel;

namespace Amuse.Modules.Billing.Contracts;

public interface IFxRateReadModel
{
    Task<Result<(FxRate Rate, long UsdEquivalentMinor)>> GetUsdEquivalentAsync(
        string currency,
        long amountMinor,
        CancellationToken cancellationToken);
}
