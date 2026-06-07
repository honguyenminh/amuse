using Amuse.Modules.Catalog.Features.Common;
using Amuse.Modules.Catalog.Features.Shared;
using Amuse.Modules.Common.Authorization;
using Amuse.Modules.Common.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Catalog.Features.ManageTracks;

public static class ManageTracksEndpoint
{
    public static IEndpointRouteBuilder MapManageTracksEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/v1/catalog/releases/{releaseId:guid}/tracks", async (
                Guid releaseId,
                CreateTrackRequest request,
                CreateTrackHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(releaseId, request, httpContext.User, cancellationToken);
                return result.ToResult(response => Results.Created(
                    $"/api/v1/catalog/tracks/{response.Id}",
                    response));
            })
            .RequireAuthorization(OrgPolicies.WriteDraftCatalog)
            .WithRequestValidation()
            .WithName("CreateCatalogTrack")
            .WithSummary("Add a track to a draft release.")
            .Produces<ManageTrackResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        endpoints.MapPatch("/api/v1/catalog/tracks/{trackId:guid}", async (
                Guid trackId,
                UpdateTrackRequest request,
                UpdateTrackHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(trackId, request, httpContext.User, cancellationToken);
                return result.ToResult(Results.Ok);
            })
            .RequireAuthorization(OrgPolicies.WriteDraftCatalog)
            .WithRequestValidation()
            .WithName("UpdateCatalogTrack")
            .WithSummary("Update track metadata on a draft release.")
            .Produces<ManageTrackResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        endpoints.MapDelete("/api/v1/catalog/tracks/{trackId:guid}", async (
                Guid trackId,
                DeleteTrackHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(trackId, httpContext.User, cancellationToken);
                return result.ToResult();
            })
            .RequireAuthorization(OrgPolicies.WriteDraftCatalog)
            .WithName("DeleteCatalogTrack")
            .WithSummary("Delete a track from an unpublished release and remove its media objects.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return endpoints;
    }
}
