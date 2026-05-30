using Amuse.Modules.Common.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Identity.Features.ResendConfirmation;

public static class ResendConfirmationEndpoint
{
    public static RouteGroupBuilder MapResendConfirmationEndpoint(this RouteGroupBuilder group)
    {
        group.MapPost("/resend-confirmation", async (
                ResendConfirmationRequest request,
                ResendConfirmationHandler handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(request, cancellationToken);
                return result.ToResult(body => Results.Ok(body));
            })
            .WithRequestValidation()
            .WithName("ResendConfirmation")
            .WithSummary("Resend email confirmation link.")
            .Produces<ResendConfirmationResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesValidationProblem();

        return group;
    }
}
