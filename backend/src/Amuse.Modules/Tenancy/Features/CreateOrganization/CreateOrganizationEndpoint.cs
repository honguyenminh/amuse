using Amuse.Modules.Common.Endpoints;
using Amuse.Modules.Tenancy.Features.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Tenancy.Features.CreateOrganization;

public static class CreateOrganizationEndpoint
{
    public static RouteGroupBuilder MapCreateOrganizationEndpoint(this RouteGroupBuilder group)
    {
        group.MapPost("/organizations", async (
                CreateOrganizationRequest request,
                CreateOrganizationHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(request, httpContext.User, cancellationToken);
                return result.ToTenancyResult(response => Results.Created(
                    $"/api/v1/tenancy/organizations/{response.Id}",
                    response));
            })
            .RequireAuthorization()
            .WithRequestValidation()
            .WithName("CreateOrganization")
            .WithSummary("Create an organization and assign the caller as owner.")
            .WithDescription(
                "Indie groups self-activate with restricted capabilities. Backing organizations enter pending_review until platform approval, unless the creator is a platform operator with instant-approve claims (platform:root, review:platform:organizations, or manage:platform:organizations).")
            .Produces<OrganizationResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesValidationProblem();

        return group;
    }
}
