using Amuse.Modules.Billing.Features.Common;
using Amuse.Modules.Billing.Features.PayoutProfile;
using Amuse.Modules.Common.Authorization;
using Amuse.Modules.Common.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Billing.Features.PayoutProfile;

public static class PayoutProfileEndpoint
{
    public static IEndpointRouteBuilder MapPayoutProfileEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/billing/payout-profile", async (
                GetPayoutProfileHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(httpContext.User, cancellationToken);
                return result.ToBillingResult(Results.Ok);
            })
            .RequireAuthorization(OrgPolicies.ReadPayout)
            .WithName("GetPayoutProfile")
            .WithSummary(
                "Get the organization payout profile (Gate B). Returns billing.payout_profile.not_found when unset.")
            .Produces<PayoutProfileResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        endpoints.MapPut("/api/v1/billing/payout-profile", async (
                UpsertPayoutProfileRequest request,
                UpsertPayoutProfileHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(request, httpContext.User, cancellationToken);
                return result.ToBillingResult(Results.Ok);
            })
            .RequireAuthorization(OrgPolicies.ManagePayoutProfile)
            .WithRequestValidation()
            .WithName("UpsertPayoutProfile")
            .WithSummary(
                "Create or update payout profile details. Material changes after verification move status to under_review and block withdrawals.")
            .Produces<PayoutProfileResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        endpoints.MapPost("/api/v1/billing/payout-profile/submit", async (
                SubmitPayoutProfileHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(httpContext.User, cancellationToken);
                return result.ToBillingResult(Results.Ok);
            })
            .RequireAuthorization(OrgPolicies.ManagePayoutProfile)
            .WithName("SubmitPayoutProfile")
            .WithSummary(
                "Submit payout profile for platform review. Returns billing.payout_profile.incomplete or billing.payout_profile.invalid_status_transition when invalid.")
            .Produces<PayoutProfileResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        endpoints.MapPost("/api/v1/billing/payout-profile/stripe-account-link", async (
                CreateStripeAccountLinkHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(httpContext.User, cancellationToken);
                return result.ToBillingResult(Results.Ok);
            })
            .RequireAuthorization(OrgPolicies.ManagePayoutProfile)
            .WithName("CreateStripeAccountLink")
            .WithSummary(
                "Create a Stripe Account Link for stripe_global payout rail onboarding. Returns billing.payout.not_configured when Stripe is not configured.")
            .Produces<StripeAccountLinkResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return endpoints;
    }
}
