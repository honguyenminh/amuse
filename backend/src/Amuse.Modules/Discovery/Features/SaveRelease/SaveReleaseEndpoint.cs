using Amuse.Modules.Common.Authorization;
using Amuse.Modules.Discovery.Features.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Discovery.Features.SaveRelease;

public static class SaveReleaseEndpoint
{
    public static IEndpointRouteBuilder MapSaveReleaseEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPut("/api/v1/discovery/library/releases/{releaseId:guid}", async (
                Guid releaseId,
                SaveReleaseHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(releaseId, httpContext.User, cancellationToken);
                return result.ToDiscoveryResult();
            })
            .RequireAuthorization(PersonaPolicies.RequireListenerPersona)
            .WithName("SaveRelease")
            .WithSummary("Save a published release to the listener library.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        return endpoints;
    }
}
