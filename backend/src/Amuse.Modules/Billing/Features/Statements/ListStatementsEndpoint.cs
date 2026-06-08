using Amuse.Modules.Billing.Features.Common;
using Amuse.Modules.Common.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Billing.Features.Statements;

public static class ListStatementsEndpoint
{
    public static IEndpointRouteBuilder MapListStatementsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/billing/statements", async (
                int? page,
                int? pageSize,
                ListStatementsHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(page, pageSize, httpContext.User, cancellationToken);
                return result.ToBillingResult(Results.Ok);
            })
            .RequireAuthorization(OrgPolicies.ReadPayout)
            .WithName("ListBillingStatements")
            .WithSummary("List paginated purchase credit statement lines for the organization.")
            .Produces<PagedStatementsResponse>();

        return endpoints;
    }
}
