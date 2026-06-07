using Amuse.Modules.Catalog.Features.Common;
using Amuse.Modules.Catalog.Features.Shared;
using Amuse.Modules.Common.Authorization;
using Amuse.Modules.Common.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Catalog.Features.ManageArtistAvatar;

public static class ManageArtistAvatarEndpoint
{
    public static IEndpointRouteBuilder MapManageArtistAvatarEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/v1/catalog/artists/{artistId:guid}/avatar/presign-upload", async (
                Guid artistId,
                PresignArtistAvatarUploadRequest request,
                PresignArtistAvatarUploadHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(artistId, request, httpContext.User, cancellationToken);
                return result.ToResult(Results.Ok);
            })
            .RequireAuthorization(OrgPolicies.UploadCatalog)
            .WithRequestValidation()
            .WithName("PresignCatalogArtistAvatarUpload")
            .WithSummary("Return a short-lived presigned PUT URL for uploading an artist profile picture.")
            .Produces<PresignArtistAvatarUploadResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        endpoints.MapPost("/api/v1/catalog/artists/{artistId:guid}/avatar/complete", async (
                Guid artistId,
                CompleteArtistAvatarUploadRequest request,
                CompleteArtistAvatarUploadHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(artistId, request, httpContext.User, cancellationToken);
                return result.ToResult(Results.Ok);
            })
            .RequireAuthorization(OrgPolicies.UploadCatalog)
            .WithRequestValidation()
            .WithName("CompleteCatalogArtistAvatarUpload")
            .WithSummary("Mark an artist avatar upload as complete and attach it to the artist profile.")
            .Produces<CompleteArtistAvatarUploadResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return endpoints;
    }
}
