using Amuse.Domain.Identity;
using Amuse.Domain.Platform;
using Amuse.Modules.Platform.Contracts;
using Amuse.Modules.Platform.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Platform.Services;

internal sealed class PlatformOperatorLookup(PlatformDbContext dbContext) : IPlatformOperatorLookup
{
    public async Task<PlatformOperatorId?> GetOperatorIdForAccountAsync(
        AccountId accountId,
        CancellationToken cancellationToken)
    {
        var operatorId = await dbContext.PlatformOperators
            .AsNoTracking()
            .Where(o => o.AccountId == accountId)
            .Select(o => (int?)o.Id.Value)
            .FirstOrDefaultAsync(cancellationToken);

        return operatorId is null ? null : PlatformOperatorId.From(operatorId.Value);
    }
}
