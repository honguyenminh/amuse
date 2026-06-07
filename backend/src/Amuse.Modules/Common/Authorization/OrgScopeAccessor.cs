using System.Security.Claims;
using Amuse.Domain.Tenancy;
using Microsoft.AspNetCore.Http;

namespace Amuse.Modules.Common.Authorization;

public sealed class OrgScopeAccessor(IHttpContextAccessor httpContextAccessor) : IOrgScopeAccessor
{
    public OrganizationId? CurrentOrganizationId
    {
        get
        {
            var user = httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
                return null;

            var ctx = user.FindFirst("ctx")?.Value;
            if (!string.Equals(ctx, "org", StringComparison.OrdinalIgnoreCase))
                return null;

            var orgId = user.FindFirst("org_id")?.Value;
            if (string.IsNullOrWhiteSpace(orgId) || !Guid.TryParse(orgId, out var organizationGuid))
                return null;

            return OrganizationId.From(organizationGuid);
        }
    }
}

internal sealed class NullOrgScopeAccessor : IOrgScopeAccessor
{
    public static readonly NullOrgScopeAccessor Instance = new();

    public OrganizationId? CurrentOrganizationId => null;
}
