using Amuse.Modules.Common.Authorization;
using Amuse.Modules.Common.Endpoints;
using Amuse.Modules.Tenancy.Features.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Tenancy.Features.UpdatePortalProfile;

public static class UpdatePortalProfileEndpoint
{
    public static RouteGroupBuilder MapUpdatePortalProfileEndpoint(this RouteGroupBuilder group)
    {
        group.MapPatch("/portal-profile", async (
                UpdateBusinessPortalProfileRequest request,
                UpdatePortalProfileHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(request, httpContext.User, cancellationToken);
                return result.ToTenancyResult(Results.Ok);
            })
            .RequireAuthorization(PersonaPolicies.RequireBusinessPortalPersona)
            .WithRequestValidation()
            .WithName("UpdateBusinessPortalProfile")
            .WithSummary("Update the signed-in account business portal profile.")
            .Produces<BusinessPortalProfileResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        return group;
    }
}
