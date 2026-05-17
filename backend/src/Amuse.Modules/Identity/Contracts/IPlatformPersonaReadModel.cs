using Amuse.Domain.Identity;
using Amuse.Domain.SharedKernel;

namespace Amuse.Modules.Identity.Contracts;

public interface IPlatformPersonaReadModel
{
    Task<Result<PersonaAccessContext>> GetPlatformContextAsync(
        AccountId accountId,
        CancellationToken cancellationToken);

    Task<bool> IsPlatformOperatorAsync(AccountId accountId, CancellationToken cancellationToken);
}
