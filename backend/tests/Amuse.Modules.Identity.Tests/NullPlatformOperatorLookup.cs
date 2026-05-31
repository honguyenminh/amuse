using Amuse.Domain.Identity;
using Amuse.Domain.Platform;
using Amuse.Modules.Platform.Contracts;

namespace Amuse.Modules.Identity.Tests;

internal sealed class NullPlatformOperatorLookup : IPlatformOperatorLookup
{
    public static readonly NullPlatformOperatorLookup Instance = new();

    public Task<PlatformOperatorId?> GetOperatorIdForAccountAsync(
        AccountId accountId,
        CancellationToken cancellationToken) =>
        Task.FromResult<PlatformOperatorId?>(null);

    public Task<IReadOnlyList<string>?> GetEffectiveClaimsForAccountAsync(
        AccountId accountId,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<string>?>(null);
}
