using Amuse.Modules.Common.Authorization;
using Amuse.Modules.Discovery.Features.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Discovery.Features.ListLibraryLiked;

public static class ListLibraryLikedEndpoint
{
    public static IEndpointRouteBuilder MapListLibraryLikedEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/discovery/library/liked", async (
                ListLibraryLikedHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(httpContext.User, cancellationToken);
                return result.ToDiscoveryResult(Results.Ok);
            })
            .RequireAuthorization(PersonaPolicies.RequireListenerPersona)
            .WithName("ListLibraryLiked")
            .WithSummary("List liked tracks with catalog metadata.")
            .Produces<LikedTracksResponse>()
            .ProducesProblem(StatusCodes.Status403Forbidden);

        return endpoints;
    }
}
