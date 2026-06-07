using Amuse.Modules.Common.Authorization;
using Amuse.Modules.Discovery.Features.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Discovery.Features.ListMyPlaylists;

public static class ListMyPlaylistsEndpoint
{
    public static IEndpointRouteBuilder MapListMyPlaylistsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/discovery/playlists/mine", async (
                ListMyPlaylistsHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(httpContext.User, cancellationToken);
                return result.ToDiscoveryResult(Results.Ok);
            })
            .RequireAuthorization(PersonaPolicies.RequireListenerPersona)
            .WithName("ListMyPlaylists")
            .WithSummary("List playlists owned by the authenticated listener.")
            .Produces<PlaylistListResponse>()
            .ProducesProblem(StatusCodes.Status403Forbidden);

        return endpoints;
    }
}
