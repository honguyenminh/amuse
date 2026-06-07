using Amuse.Modules.Tenancy.Features.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Tenancy.Features.DeclineInvite;

public static class DeclineInviteEndpoint
{
    public static RouteGroupBuilder MapDeclineInviteEndpoint(this RouteGroupBuilder group)
    {
        group.MapPost("/invites/{token}/decline", async (
                string token,
                DeclineInviteHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(token, httpContext.User, cancellationToken);
                return result.ToTenancyResult();
            })
            .RequireAuthorization()
            .WithName("DeclineOrganizationInvite")
            .WithSummary("Decline an organization invite for the signed-in account.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status410Gone);

        return group;
    }
}
