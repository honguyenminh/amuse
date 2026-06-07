using Amuse.Modules.Common.Authorization;
using Amuse.Modules.Common.Endpoints;
using Amuse.Modules.Discovery.Features.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Discovery.Features.GetLikedPlaylist;

public static class GetLikedPlaylistEndpoint
{
    public static IEndpointRouteBuilder MapGetLikedPlaylistEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/discovery/liked", async (
                GetLikedPlaylistHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(httpContext.User, cancellationToken);
                return result.ToDiscoveryResult(Results.Ok);
            })
            .RequireAuthorization(PersonaPolicies.RequireListenerPersona)
            .WithName("GetLikedPlaylist")
            .WithSummary("Get the listener's liked collection as a playlist.")
            .Produces<PlaylistDetailDto>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        return endpoints;
    }
}
