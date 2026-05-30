using Amuse.Domain.Tenancy;

namespace Amuse.Modules.Tenancy.Features.CreateOrganization;

public sealed record CreateOrganizationRequest(string DisplayName, OrganizationClass OrgClass);
