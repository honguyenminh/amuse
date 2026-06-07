using Amuse.Modules.Common.Authorization;
using Amuse.Modules.Common.Endpoints;
using Amuse.Modules.Tenancy.Features.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Tenancy.Features.GetPortalProfile;

public static class GetPortalProfileEndpoint
{
    public static RouteGroupBuilder MapGetPortalProfileEndpoint(this RouteGroupBuilder group)
    {
        group.MapGet("/portal-profile", async (
                GetPortalProfileHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(httpContext.User, cancellationToken);
                return result.ToTenancyResult(Results.Ok);
            })
            .RequireAuthorization(PersonaPolicies.RequireBusinessPortalPersona)
            .WithName("GetBusinessPortalProfile")
            .WithSummary("Get the signed-in account business portal profile.")
            .Produces<BusinessPortalProfileResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        return group;
    }
}
