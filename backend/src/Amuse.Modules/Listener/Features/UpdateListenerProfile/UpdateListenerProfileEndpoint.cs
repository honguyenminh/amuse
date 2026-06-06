using Amuse.Modules.Common.Authorization;
using Amuse.Modules.Common.Endpoints;
using Amuse.Modules.Listener.Features.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Listener.Features.UpdateListenerProfile;

public static class UpdateListenerProfileEndpoint
{
    public static IEndpointRouteBuilder MapUpdateListenerProfileEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPatch("/api/v1/listener/profile", async (
                UpdateListenerProfileRequest request,
                UpdateListenerProfileHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(request, httpContext.User, cancellationToken);
                return result.ToResult(Results.Ok);
            })
            .RequireAuthorization(PersonaPolicies.RequireListenerPersona)
            .WithRequestValidation()
            .WithName("UpdateListenerProfile")
            .WithSummary("Update listener profile presentation and onboarding preferences.")
            .Produces<ListenerProfileResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        return endpoints;
    }
}
