using Amuse.Domain.Catalog;
using Amuse.Modules.Catalog.Features.Common;
using Amuse.Modules.Common.Authorization;
using Amuse.Modules.Common.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Catalog.Features.ManagePricing;

public static class ManagePricingEndpoint
{
    public static IEndpointRouteBuilder MapManagePricingEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPatch("/api/v1/catalog/tracks/{trackId:guid}/pricing", async (
                Guid trackId,
                SetTrackPricingRequest request,
                SetTrackPricingHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(trackId, request, httpContext.User, cancellationToken);
                return result.ToResult(Results.Ok);
            })
            .RequireAuthorization(OrgPolicies.ManageCatalogPricing)
            .WithRequestValidation()
            .WithName("SetCatalogTrackPricing")
            .WithSummary(
                "Set pay-what-you-want pricing for a track. Returns catalog.invalid_pricing, catalog.invalid_pricing_bounds, catalog.invalid_pricing_currency, or catalog.pricing_changes_blocked when validation fails.")
            .Produces<ManageTrackResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        endpoints.MapPatch("/api/v1/catalog/releases/{releaseId:guid}/pricing", async (
                Guid releaseId,
                SetReleasePricingRequest request,
                SetReleasePricingHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(releaseId, request, httpContext.User, cancellationToken);
                return result.ToResult(Results.Ok);
            })
            .RequireAuthorization(OrgPolicies.ManageCatalogPricing)
            .WithRequestValidation()
            .WithName("SetCatalogReleasePricing")
            .WithSummary(
                "Set pay-what-you-want pricing for a release bundle. Returns catalog.invalid_pricing, catalog.invalid_pricing_bounds, catalog.invalid_pricing_currency, or catalog.pricing_changes_blocked when validation fails.")
            .Produces<ManageReleaseDetailResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        endpoints.MapPut("/api/v1/catalog/tracks/{trackId:guid}/royalty-splits", async (
                Guid trackId,
                SetTrackRoyaltySplitsRequest request,
                SetTrackRoyaltySplitsHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(trackId, request, httpContext.User, cancellationToken);
                return result.ToResult(Results.Ok);
            })
            .RequireAuthorization(OrgPolicies.ManageCatalogPricing)
            .WithRequestValidation()
            .WithName("SetCatalogTrackRoyaltySplits")
            .WithSummary(
                "Replace royalty split rows for a track. Empty list clears explicit splits (defaults to 100% listing org at purchase). Returns catalog.royalty_split_sum_invalid or catalog.duplicate_royalty_payee when invalid.")
            .Produces<ManageTrackResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return endpoints;
    }
}
