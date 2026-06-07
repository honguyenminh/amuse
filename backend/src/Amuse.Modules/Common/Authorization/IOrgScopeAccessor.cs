using Amuse.Domain.Tenancy;

namespace Amuse.Modules.Common.Authorization;

public interface IOrgScopeAccessor
{
    OrganizationId? CurrentOrganizationId { get; }
}
