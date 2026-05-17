using Amuse.Domain.Identity;
using Amuse.Domain.SharedKernel;
using Amuse.Domain.Tenancy;

namespace Amuse.Modules.Identity.Contracts;

public interface ITenancyPersonaReadModel
{
    Task<Result<PersonaAccessContext>> GetOrgContextAsync(
        AccountId accountId,
        OrganizationId organizationId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<OrgPersonaListing>> ListAvailableOrgsAsync(
        AccountId accountId,
        CancellationToken cancellationToken);
}
