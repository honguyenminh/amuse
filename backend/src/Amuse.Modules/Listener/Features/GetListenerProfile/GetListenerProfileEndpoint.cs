using Amuse.Modules.Common.Endpoints;
using Amuse.Modules.Common.Authorization;
using Amuse.Modules.Listener.Features.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Listener.Features.GetListenerProfile;

public static class GetListenerProfileEndpoint
{
    public static IEndpointRouteBuilder MapGetListenerProfileEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/listener/profile", async (
                GetListenerProfileHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(httpContext.User, cancellationToken);
                return result.ToResult(Results.Ok);
            })
            .RequireAuthorization(PersonaPolicies.RequireListenerPersona)
            .WithName("GetListenerProfile")
            .WithSummary("Get the signed-in listener profile and preferences.")
            .Produces<ListenerProfileResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        return endpoints;
    }
}
