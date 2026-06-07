using Amuse.Modules.Discovery.Features.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Discovery.Features.Search;

public static class SearchEndpoint
{
    public static IEndpointRouteBuilder MapSearchEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/discovery/search", async (
                string? q,
                int? pageSize,
                SearchHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(q, pageSize, httpContext.User, cancellationToken);
                return result.ToDiscoveryResult(Results.Ok);
            })
            .AllowAnonymous()
            .WithName("DiscoverySearch")
            .WithSummary("Search catalog content and public playlists. Optional auth respects listener unverified-artist preference.")
            .Produces<SearchResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return endpoints;
    }
}
