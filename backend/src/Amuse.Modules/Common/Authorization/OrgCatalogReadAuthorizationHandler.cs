using Amuse.Domain.Tenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Common.Authorization;

public sealed class OrgCatalogReadAuthorizationHandler(IHttpContextAccessor httpContextAccessor)
    : AuthorizationHandler<OrgCatalogReadRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OrgCatalogReadRequirement requirement)
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

        foreach (var claim in context.User.FindAll("claims").Select(c => c.Value))
        {
            if (!OrgClaim.TryParse(claim, out var parsed))
                continue;

            if (string.Equals(parsed.Scope, "catalog", StringComparison.Ordinal)
                && string.Equals(parsed.Action, "read", StringComparison.Ordinal))
            {
                context.Succeed(requirement);
                break;
            }
        }

        return Task.CompletedTask;
    }
}
