using Amuse.Modules.Tenancy.Features.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Tenancy.Features.AcceptInvite;

public static class AcceptInviteEndpoint
{
    public static RouteGroupBuilder MapAcceptInviteEndpoint(this RouteGroupBuilder group)
    {
        group.MapPost("/invites/{token}/accept", async (
                string token,
                AcceptInviteHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(token, httpContext.User, cancellationToken);
                return result.ToTenancyResult(Results.Ok);
            })
            .RequireAuthorization()
            .WithName("AcceptOrganizationInvite")
            .WithSummary("Accept an organization invite for the signed-in account.")
            .Produces<AcceptInviteResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status410Gone);

        return group;
    }
}
