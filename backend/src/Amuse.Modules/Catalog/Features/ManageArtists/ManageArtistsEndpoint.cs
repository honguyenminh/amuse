using Amuse.Modules.Catalog.Features.Shared;
using Amuse.Modules.Common.Authorization;
using Amuse.Modules.Common.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Catalog.Features.ManageArtists;

public static class ManageArtistsEndpoint
{
    public static IEndpointRouteBuilder MapManageArtistsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/v1/catalog/artists", async (
                CreateArtistRequest request,
                CreateArtistHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(request, httpContext.User, cancellationToken);
                return result.ToResult(response => Results.Created(
                    $"/api/v1/catalog/artists/{response.Id}",
                    response));
            })
            .RequireAuthorization(OrgPolicies.WriteDraftCatalog)
            .WithRequestValidation()
            .WithName("CreateCatalogArtist")
            .WithSummary(
                "Add an artist to the organization roster. Returns 400 with catalog.invalid_slug or catalog.duplicate_slug when the slug is invalid or already taken. Bio supports a limited markdown subset; returns catalog.invalid_formatted_text when unsupported formatting or invalid links are present.")
            .Produces<ManageArtistSummaryResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        endpoints.MapGet("/api/v1/catalog/manage/artists/slug-availability", async (
                string slug,
                CheckArtistSlugAvailabilityHandler handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(slug, cancellationToken);
                return result.ToResult(Results.Ok);
            })
            .RequireAuthorization(OrgPolicies.WriteDraftCatalog)
            .WithName("CheckCatalogArtistSlugAvailability")
            .WithSummary("Check whether an artist slug is valid and available globally.")
            .Produces<ArtistSlugAvailabilityResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        endpoints.MapGet("/api/v1/catalog/manage/artists", async (
                ListArtistsHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(httpContext.User, cancellationToken);
                return result.ToResult(Results.Ok);
            })
            .RequireAuthorization(OrgPolicies.ReadCatalog)
            .WithName("ListCatalogArtists")
            .WithSummary("List artists managed by the active organization.")
            .Produces<ManageArtistListResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        endpoints.MapGet("/api/v1/catalog/manage/artists/{artistId:guid}", async (
                Guid artistId,
                GetArtistHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(artistId, httpContext.User, cancellationToken);
                return result.ToResult(Results.Ok);
            })
            .RequireAuthorization(OrgPolicies.ReadCatalog)
            .WithName("GetCatalogArtistOrgView")
            .WithSummary("Get organization-scoped artist detail including release lifecycle and track explicit flags.")
            .Produces<ManageArtistDetailResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        endpoints.MapPatch("/api/v1/catalog/artists/{artistId:guid}", async (
                Guid artistId,
                UpdateArtistRequest request,
                UpdateArtistHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(artistId, request, httpContext.User, cancellationToken);
                return result.ToResult(Results.Ok);
            })
            .RequireAuthorization(OrgPolicies.WriteDraftCatalog)
            .WithRequestValidation()
            .WithName("UpdateCatalogArtist")
            .WithSummary(
                "Update artist profile metadata. Bio supports a limited markdown subset; returns catalog.invalid_formatted_text when unsupported formatting or invalid links are present.")
            .Produces<ManageArtistSummaryResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return endpoints;
    }
}
