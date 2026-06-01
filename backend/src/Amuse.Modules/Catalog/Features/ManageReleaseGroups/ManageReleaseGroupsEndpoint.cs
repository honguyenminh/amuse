using Amuse.Modules.Common.Authorization;
using Amuse.Modules.Common.Endpoints;
using Amuse.Modules.Catalog.Features.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Catalog.Features.ManageReleaseGroups;

public static class ManageReleaseGroupsEndpoint
{
    public static IEndpointRouteBuilder MapManageReleaseGroupsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/v1/catalog/artists/{artistId:guid}/release-groups", async (
                Guid artistId,
                CreateReleaseGroupRequest request,
                CreateReleaseGroupHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(artistId, request, httpContext.User, cancellationToken);
                return result.ToResult(response => Results.Created(
                    $"/api/v1/catalog/artists/{artistId}/release-groups/{response.Id}",
                    response));
            })
            .RequireAuthorization(OrgPolicies.WriteDraftCatalog)
            .WithRequestValidation()
            .WithName("CreateCatalogArtistReleaseGroup")
            .WithSummary("Create a release group for an artist.")
            .Produces<ManageReleaseGroupResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        endpoints.MapGet("/api/v1/catalog/artists/{artistId:guid}/release-groups", async (
                Guid artistId,
                ListReleaseGroupsHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(artistId, httpContext.User, cancellationToken);
                return result.ToResult(Results.Ok);
            })
            .RequireAuthorization(OrgPolicies.ReadCatalog)
            .WithName("ListCatalogArtistReleaseGroups")
            .WithSummary("List release groups for an artist.")
            .Produces<ManageReleaseGroupListResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        endpoints.MapGet("/api/v1/catalog/artists/{artistId:guid}/release-groups/{releaseGroupId:guid}", async (
                Guid artistId,
                Guid releaseGroupId,
                GetReleaseGroupDetailHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(
                    artistId,
                    releaseGroupId,
                    httpContext.User,
                    cancellationToken);
                return result.ToResult(Results.Ok);
            })
            .RequireAuthorization(OrgPolicies.ReadCatalog)
            .WithName("GetCatalogArtistReleaseGroupDetail")
            .WithSummary("Get a release group with member releases for catalog management.")
            .Produces<ManageReleaseGroupDetailResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        endpoints.MapPatch("/api/v1/catalog/artists/{artistId:guid}/release-groups/{releaseGroupId:guid}", async (
                Guid artistId,
                Guid releaseGroupId,
                UpdateReleaseGroupRequest request,
                UpdateReleaseGroupHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(
                    artistId,
                    releaseGroupId,
                    request,
                    httpContext.User,
                    cancellationToken);
                return result.ToResult(Results.Ok);
            })
            .RequireAuthorization(OrgPolicies.WriteDraftCatalog)
            .WithRequestValidation()
            .WithName("UpdateCatalogArtistReleaseGroup")
            .WithSummary("Update release group metadata.")
            .Produces<ManageReleaseGroupResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return endpoints;
    }
}
