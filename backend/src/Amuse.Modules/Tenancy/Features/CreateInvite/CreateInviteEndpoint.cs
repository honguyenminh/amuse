using Amuse.Modules.Common.Authorization;
using Amuse.Modules.Common.Endpoints;
using Amuse.Modules.Tenancy.Features.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Tenancy.Features.CreateInvite;

public static class CreateInviteEndpoint
{
    public static RouteGroupBuilder MapCreateInviteEndpoint(this RouteGroupBuilder group)
    {
        group.MapPost("/organizations/{organizationId:guid}/members/invites", async (
                Guid organizationId,
                CreateInviteRequest request,
                CreateInviteHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(
                    organizationId,
                    request,
                    httpContext.User,
                    cancellationToken);
                return result.ToTenancyResult(response =>
                    Results.Created(
                        $"/api/v1/tenancy/organizations/{organizationId}/members/invites/{response.InviteId}",
                        response));
            })
            .RequireAuthorization(OrgPolicies.ManageMembership)
            .RequireOrgTenant()
            .WithRequestValidation()
            .WithName("CreateOrganizationInvite")
            .WithSummary("Invite a member by email.")
            .Produces<CreateOrganizationInviteResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return group;
    }
}
