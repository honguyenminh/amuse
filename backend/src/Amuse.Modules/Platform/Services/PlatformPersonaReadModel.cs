using Amuse.Domain.Identity;
using Amuse.Domain.Platform;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Identity.Contracts;
using Amuse.Modules.Platform.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Platform.Services;

internal sealed class PlatformPersonaReadModel(PlatformDbContext dbContext) : IPlatformPersonaReadModel
{
    public async Task<Result<PersonaAccessContext>> GetPlatformContextAsync(
        AccountId accountId,
        CancellationToken cancellationToken)
    {
        var op = await dbContext.PlatformOperators
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.AccountId == accountId, cancellationToken);

        if (op is null)
            return Result<PersonaAccessContext>.Failure(IdentityErrors.InvalidPersonaContext);

        var claims = op.Id == PlatformOperatorId.Root
            ? op.Claims.Concat(["platform:root"]).Distinct().ToList()
            : op.Claims.ToList();

        return Result<PersonaAccessContext>.Success(new PersonaAccessContext(
            "platform",
            null,
            null,
            null,
            claims));
    }

    public async Task<bool> IsPlatformOperatorAsync(AccountId accountId, CancellationToken cancellationToken) =>
        await dbContext.PlatformOperators.AsNoTracking().AnyAsync(o => o.AccountId == accountId, cancellationToken);
}
