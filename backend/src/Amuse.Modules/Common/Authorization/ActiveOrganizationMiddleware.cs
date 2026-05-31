using Amuse.Domain.Tenancy;
using Amuse.Modules.Tenancy.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Common.Authorization;

/// <summary>
/// Blocks org-tenant API calls when the route organization is soft-deleted (closed).
/// </summary>
public sealed class ActiveOrganizationMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, TenancyDbContext dbContext)
    {
        var endpoint = context.GetEndpoint();
        if (endpoint?.Metadata.GetMetadata<RequireOrgTenantAttribute>() is null)
        {
            await next(context);
            return;
        }

        var routeOrgId = context.GetRouteValue("organizationId")?.ToString();
        if (!Guid.TryParse(routeOrgId, out var organizationId))
        {
            await next(context);
            return;
        }

        var isActive = await dbContext.Organizations.AsNoTracking()
            .AnyAsync(
                o => o.Id == OrganizationId.From(organizationId)
                     && o.LifecycleStatus != OrganizationLifecycleStatus.Closed,
                context.RequestAborted);

        if (!isActive)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(new
            {
                type = "https://tools.ietf.org/html/rfc9110#section-15.5.5",
                title = TenancyErrors.OrganizationNotFound.Code,
                status = StatusCodes.Status404NotFound,
                detail = TenancyErrors.OrganizationNotFound.Message,
                code = TenancyErrors.OrganizationNotFound.Code,
            }, context.RequestAborted);
            return;
        }

        await next(context);
    }
}
