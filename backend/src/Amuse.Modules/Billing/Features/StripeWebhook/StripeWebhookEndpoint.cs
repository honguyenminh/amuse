using Amuse.Modules.Billing.Features.Common;
using Amuse.Modules.Common.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Billing.Features.StripeWebhook;

public static class StripeWebhookEndpoint
{
    public static IEndpointRouteBuilder MapStripeWebhookEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/v1/billing/webhooks/stripe", async (
                HttpContext httpContext,
                StripeWebhookHandler handler,
                CancellationToken cancellationToken) =>
            {
                using var reader = new StreamReader(httpContext.Request.Body);
                var json = await reader.ReadToEndAsync(cancellationToken);
                var signature = httpContext.Request.Headers["Stripe-Signature"].ToString();

                var result = await handler.HandleAsync(json, signature, cancellationToken);
                return result.ToBillingResult(() => Results.Ok(new { received = true }));
            })
            .AllowAnonymous()
            .DisableAntiforgery()
            .WithName("StripeWebhook")
            .WithSummary("Stripe payment webhook for checkout completion and refunds.")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return endpoints;
    }
}
