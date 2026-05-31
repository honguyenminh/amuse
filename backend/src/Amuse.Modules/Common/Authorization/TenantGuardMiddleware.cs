using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Common.Authorization;

public sealed class TenantGuardMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        var requiresTenant = endpoint?.Metadata.GetMetadata<RequireOrgTenantAttribute>() is not null;

        if (requiresTenant)
        {
            var orgId = context.User.FindFirst("org_id")?.Value;
            var ctx = context.User.FindFirst("ctx")?.Value;
            if (!string.Equals(ctx, "org", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(orgId))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }

            var routeOrgId = context.GetRouteValue("organizationId")?.ToString();
            if (!string.IsNullOrWhiteSpace(routeOrgId)
                && !string.Equals(routeOrgId, orgId, StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }
        }

        await next(context);
    }
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class RequireOrgTenantAttribute : Attribute;
