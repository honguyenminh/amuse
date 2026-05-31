using Amuse.Domain.Tenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Common.Authorization;

public sealed class OrgClaimAuthorizationHandler(IHttpContextAccessor httpContextAccessor)
    : AuthorizationHandler<OrgClaimRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OrgClaimRequirement requirement)
    {
        if (!string.Equals(context.User.FindFirst("ctx")?.Value, "org", StringComparison.OrdinalIgnoreCase))
            return Task.CompletedTask;

        var jwtOrgId = context.User.FindFirst("org_id")?.Value;
        if (string.IsNullOrWhiteSpace(jwtOrgId))
            return Task.CompletedTask;

        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext is not null)
        {
            var routeOrgId = httpContext.GetRouteValue("organizationId")?.ToString()
                ?? httpContext.GetRouteValue("orgId")?.ToString();
            if (!string.IsNullOrWhiteSpace(routeOrgId)
                && !string.Equals(routeOrgId, jwtOrgId, StringComparison.OrdinalIgnoreCase))
                return Task.CompletedTask;
        }

        var granted = context.User.FindAll("claims")
            .Select(c => c.Value)
            .ToHashSet(StringComparer.Ordinal);

        var satisfied = requirement.MatchMode switch
        {
            OrgClaimMatchMode.All => OrgClaim.MatchesAll(requirement.RequiredClaims, granted),
            _ => OrgClaim.MatchesAny(requirement.RequiredClaims, granted),
        };

        if (satisfied)
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
