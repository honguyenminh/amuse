using Amuse.Modules.Billing.Features.Common;
using Amuse.Modules.Common.Authorization;
using Amuse.Modules.Common.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Billing.Features.CreateCheckoutSession;

public static class CreateCheckoutSessionEndpoint
{
    public static IEndpointRouteBuilder MapCreateCheckoutSessionEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/v1/billing/checkout/sessions", async (
                CreateCheckoutSessionRequest request,
                CreateCheckoutSessionHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(request, httpContext.User, cancellationToken);
                return result.ToBillingResult(response => Results.Ok(response));
            })
            .RequireAuthorization(PersonaPolicies.RequireListenerPersona)
            .WithRequestValidation()
            .WithName("CreateCheckoutSession")
            .WithSummary("Create a Stripe checkout session for a paid track or release purchase.")
            .Produces<CheckoutSessionResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem();

        return endpoints;
    }
}
