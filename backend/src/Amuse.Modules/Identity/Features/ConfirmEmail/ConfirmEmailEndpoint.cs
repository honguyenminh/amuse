using Amuse.Modules.Common.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Identity.Features.ConfirmEmail;

public static class ConfirmEmailEndpoint
{
    public static RouteGroupBuilder MapConfirmEmailEndpoint(this RouteGroupBuilder group)
    {
        group.MapPost("/confirm-email", async (
                ConfirmEmailRequest request,
                ConfirmEmailHandler handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(request, cancellationToken);
                return result.ToResult();
            })
            .WithRequestValidation()
            .WithName("ConfirmEmail")
            .WithSummary("Confirm email address from registration link.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesValidationProblem();

        return group;
    }
}
