using Amuse.Modules.Common.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Catalog.Features.BrowseHome;

public static class BrowseHomeEndpoint
{
    public static IEndpointRouteBuilder MapBrowseHomeEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/catalog/home", async (
                BrowseHomeHandler handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(cancellationToken);
                return result.ToResult(Results.Ok);
            })
            .RequireAuthorization()
            .WithName("BrowseCatalogHome")
            .WithSummary("Return a curated home feed of recent albums and featured artists.")
            .Produces<BrowseHomeResponse>();

        return endpoints;
    }
}
