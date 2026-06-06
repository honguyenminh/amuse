using Amuse.Modules.Common.Authorization;
using Amuse.Modules.Discovery.Features.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Discovery.Features.ListLibraryPlaylists;

public static class ListLibraryPlaylistsEndpoint
{
    public static IEndpointRouteBuilder MapListLibraryPlaylistsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/discovery/library/playlists", async (
                ListLibraryPlaylistsHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(httpContext.User, cancellationToken);
                return result.ToDiscoveryResult(Results.Ok);
            })
            .RequireAuthorization(PersonaPolicies.RequireListenerPersona)
            .WithName("ListLibraryPlaylists")
            .WithSummary("List owned and saved playlists in the listener library.")
            .Produces<PlaylistListResponse>()
            .ProducesProblem(StatusCodes.Status403Forbidden);

        return endpoints;
    }
}
