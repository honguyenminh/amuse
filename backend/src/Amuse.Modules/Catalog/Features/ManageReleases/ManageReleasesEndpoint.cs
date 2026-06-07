using Amuse.Domain.Catalog;
using Amuse.Modules.Catalog.Features.Common;
using Amuse.Modules.Catalog.Features.Common;
using Amuse.Modules.Common.Authorization;
using Amuse.Modules.Common.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Catalog.Features.ManageReleases;

public static class ManageReleasesEndpoint
{
    public static IEndpointRouteBuilder MapManageReleasesEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/catalog/artists/{artistId:guid}/releases/slug-availability", async (
                Guid artistId,
                string slug,
                Guid? excludingReleaseId,
                CheckReleaseSlugAvailabilityHandler handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(
                    artistId,
                    slug,
                    excludingReleaseId,
                    cancellationToken);
                return result.ToResult(Results.Ok);
            })
            .RequireAuthorization(OrgPolicies.WriteDraftCatalog)
            .WithName("CheckCatalogReleaseSlugAvailability")
            .WithSummary(
                "Check whether a release slug is valid and available for the roster artist. Slugs are unique per artist.")
            .Produces<ReleaseSlugAvailabilityResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        endpoints.MapPost("/api/v1/catalog/artists/{artistId:guid}/releases", async (
                Guid artistId,
                CreateReleaseRequest request,
                CreateReleaseHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(artistId, request, httpContext.User, cancellationToken);
                return result.ToResult(response => Results.Created(
                    $"/api/v1/catalog/releases/{response.Id}",
                    response));
            })
            .RequireAuthorization(OrgPolicies.WriteDraftCatalog)
            .WithRequestValidation()
            .WithName("CreateCatalogRelease")
            .WithSummary(
                "Create a draft release for a roster artist. Optional slug; when omitted, a unique slug is generated from the title. Returns 400 with catalog.invalid_slug or catalog.duplicate_slug when the slug is invalid or already taken for this artist. Description supports a limited markdown subset; returns catalog.invalid_formatted_text when unsupported formatting or invalid links are present.")
            .Produces<ManageReleaseDetailResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        endpoints.MapGet("/api/v1/catalog/manage/releases", async (
                string? status,
                ListReleasesHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                ReleaseLifecycleStatus? parsedStatus = null;
                if (!string.IsNullOrWhiteSpace(status))
                {
                    if (!Enum.TryParse<ReleaseLifecycleStatus>(status, ignoreCase: true, out var lifecycleStatus))
                    {
                        return Results.Problem(
                            title: CatalogErrors.InvalidRelease.Code,
                            detail: "Invalid release lifecycle status filter.",
                            statusCode: StatusCodes.Status400BadRequest,
                            extensions: new Dictionary<string, object?> { ["code"] = CatalogErrors.InvalidRelease.Code });
                    }

                    parsedStatus = lifecycleStatus;
                }

                var result = await handler.HandleAsync(parsedStatus, httpContext.User, cancellationToken);
                return result.ToResult(Results.Ok);
            })
            .RequireAuthorization(OrgPolicies.ReadCatalog)
            .WithName("ListCatalogReleases")
            .WithSummary("List releases for the active organization, optionally filtered by lifecycle status.")
            .Produces<ManageReleaseListResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        endpoints.MapGet("/api/v1/catalog/manage/releases/{releaseId:guid}", async (
                Guid releaseId,
                GetReleaseHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(releaseId, httpContext.User, cancellationToken);
                return result.ToResult(Results.Ok);
            })
            .RequireAuthorization(OrgPolicies.ReadCatalog)
            .WithName("GetCatalogReleaseOrgView")
            .WithSummary("Get organization-scoped release detail with track lifecycle metadata.")
            .Produces<ManageReleaseDetailResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        endpoints.MapPatch("/api/v1/catalog/releases/{releaseId:guid}", async (
                Guid releaseId,
                UpdateReleaseRequest request,
                UpdateReleaseHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(releaseId, request, httpContext.User, cancellationToken);
                return result.ToResult(Results.Ok);
            })
            .RequireAuthorization(OrgPolicies.WriteDraftCatalog)
            .WithRequestValidation()
            .WithName("UpdateCatalogRelease")
            .WithSummary(
                "Update draft release metadata. Optional slug change while the release is still a draft. Returns 400 with catalog.invalid_slug or catalog.duplicate_slug when invalid or taken for this artist. Description supports a limited markdown subset; returns catalog.invalid_formatted_text when unsupported formatting or invalid links are present.")
            .Produces<ManageReleaseDetailResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        endpoints.MapDelete("/api/v1/catalog/releases/{releaseId:guid}", async (
                Guid releaseId,
                DeleteReleaseHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(releaseId, httpContext.User, cancellationToken);
                return result.ToResult();
            })
            .RequireAuthorization(OrgPolicies.WriteDraftCatalog)
            .WithName("DeleteCatalogRelease")
            .WithSummary("Delete an unpublished release and remove its tracks, cover art, and media objects.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return endpoints;
    }
}
