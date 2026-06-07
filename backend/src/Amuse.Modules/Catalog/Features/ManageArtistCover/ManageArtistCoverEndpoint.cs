using Amuse.Modules.Catalog.Features.Common;
using Amuse.Modules.Catalog.Features.Shared;
using Amuse.Modules.Common.Authorization;
using Amuse.Modules.Common.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Catalog.Features.ManageArtistCover;

public static class ManageArtistCoverEndpoint
{
    public static IEndpointRouteBuilder MapManageArtistCoverEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/v1/catalog/artists/{artistId:guid}/cover/presign-upload", async (
                Guid artistId,
                PresignArtistCoverUploadRequest request,
                PresignArtistCoverUploadHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(artistId, request, httpContext.User, cancellationToken);
                return result.ToResult(Results.Ok);
            })
            .RequireAuthorization(OrgPolicies.UploadCatalog)
            .WithRequestValidation()
            .WithName("PresignCatalogArtistCoverUpload")
            .WithSummary("Return a short-lived presigned PUT URL for uploading an artist cover image.")
            .Produces<PresignArtistCoverUploadResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        endpoints.MapPost("/api/v1/catalog/artists/{artistId:guid}/cover/complete", async (
                Guid artistId,
                CompleteArtistCoverUploadRequest request,
                CompleteArtistCoverUploadHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(artistId, request, httpContext.User, cancellationToken);
                return result.ToResult(Results.Ok);
            })
            .RequireAuthorization(OrgPolicies.UploadCatalog)
            .WithRequestValidation()
            .WithName("CompleteCatalogArtistCoverUpload")
            .WithSummary("Mark an artist cover upload as complete and attach it to the artist profile.")
            .Produces<CompleteArtistCoverUploadResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return endpoints;
    }
}
