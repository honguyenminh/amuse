using System.Security.Claims;
using Amuse.Domain.Identity;
using Amuse.Domain.SharedKernel;
using Amuse.Domain.Tenancy;

namespace Amuse.Modules.Billing.Features.PayoutProfile;

internal static class BillingPersonaAccessor
{
    public static Result<OrganizationId> GetOrganizationId(ClaimsPrincipal principal)
    {
        if (!string.Equals(principal.FindFirst("ctx")?.Value, "org", StringComparison.OrdinalIgnoreCase))
            return Result<OrganizationId>.Failure(IdentityErrors.InvalidPersonaContext);

        var orgIdValue = principal.FindFirst("org_id")?.Value;
        if (!Guid.TryParse(orgIdValue, out var orgId))
            return Result<OrganizationId>.Failure(IdentityErrors.InvalidPersonaContext);

        return Result<OrganizationId>.Success(OrganizationId.From(orgId));
    }
}
