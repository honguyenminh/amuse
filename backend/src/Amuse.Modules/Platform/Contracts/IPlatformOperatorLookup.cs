using Amuse.Domain.Identity;
using Amuse.Domain.Platform;

namespace Amuse.Modules.Platform.Contracts;

public interface IPlatformOperatorLookup
{
    Task<PlatformOperatorId?> GetOperatorIdForAccountAsync(
        AccountId accountId,
        CancellationToken cancellationToken);
}
