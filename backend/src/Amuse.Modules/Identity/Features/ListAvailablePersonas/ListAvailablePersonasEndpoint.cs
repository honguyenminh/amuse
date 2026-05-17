using Amuse.Modules.Common.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Identity.Features.ListAvailablePersonas;

public static class ListAvailablePersonasEndpoint
{
    public static RouteGroupBuilder MapListAvailablePersonasEndpoint(this RouteGroupBuilder group)
    {
        group.MapGet("/personas", async (
                ListAvailablePersonasHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(httpContext.User, cancellationToken);
                return result.ToResult(Results.Ok);
            })
            .RequireAuthorization()
            .WithName("ListAvailablePersonas")
            .WithSummary("List available personas for the signed-in account.")
            .Produces<IReadOnlyList<Contracts.AvailablePersona>>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return group;
    }
}
