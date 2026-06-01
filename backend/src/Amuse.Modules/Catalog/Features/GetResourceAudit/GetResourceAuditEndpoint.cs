using Amuse.Modules.Common.Authorization;
using Amuse.Modules.Common.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Catalog.Features.GetResourceAudit;

public static class GetResourceAuditEndpoint
{
    public static IEndpointRouteBuilder MapGetResourceAuditEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/catalog/manage/audit", async (
                string tableName,
                Guid targetId,
                ListResourceAuditsHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(
                    tableName,
                    targetId,
                    httpContext.User,
                    cancellationToken);
                return result.ToResult(Results.Ok);
            })
            .RequireAuthorization(OrgPolicies.ReadCatalog)
            .WithName("ListCatalogResourceAudit")
            .WithSummary("List audit history for a catalog resource owned by the active organization.")
            .Produces<CatalogAuditListResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return endpoints;
    }
}
