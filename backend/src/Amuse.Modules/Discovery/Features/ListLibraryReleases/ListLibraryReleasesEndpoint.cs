using Amuse.Modules.Common.Authorization;
using Amuse.Modules.Discovery.Features.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Discovery.Features.ListLibraryReleases;

public static class ListLibraryReleasesEndpoint
{
    public static IEndpointRouteBuilder MapListLibraryReleasesEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/discovery/library/releases", async (
                ListLibraryReleasesHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(httpContext.User, cancellationToken);
                return result.ToDiscoveryResult(Results.Ok);
            })
            .RequireAuthorization(PersonaPolicies.RequireListenerPersona)
            .WithName("ListLibraryReleases")
            .WithSummary("List saved releases with catalog metadata.")
            .Produces<SavedReleasesResponse>()
            .ProducesProblem(StatusCodes.Status403Forbidden);

        return endpoints;
    }
}
