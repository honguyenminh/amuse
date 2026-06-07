using Amuse.Modules.Catalog.Features.Common;
using Amuse.Modules.Catalog.Features.Common;
using Amuse.Modules.Common.Authorization;
using Amuse.Modules.Common.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Catalog.Features.ManageReleaseCover;

public static class ManageReleaseCoverEndpoint
{
    public static IEndpointRouteBuilder MapManageReleaseCoverEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/v1/catalog/releases/{releaseId:guid}/cover/presign-upload", async (
                Guid releaseId,
                PresignReleaseCoverUploadRequest request,
                PresignReleaseCoverUploadHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(releaseId, request, httpContext.User, cancellationToken);
                return result.ToResult(Results.Ok);
            })
            .RequireAuthorization(OrgPolicies.UploadCatalog)
            .WithRequestValidation()
            .WithName("PresignCatalogReleaseCoverUpload")
            .WithSummary("Return a short-lived presigned PUT URL for uploading release cover art.")
            .Produces<PresignReleaseCoverUploadResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        endpoints.MapPost("/api/v1/catalog/releases/{releaseId:guid}/cover/complete", async (
                Guid releaseId,
                CompleteReleaseCoverUploadRequest request,
                CompleteReleaseCoverUploadHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(releaseId, request, httpContext.User, cancellationToken);
                return result.ToResult(Results.Ok);
            })
            .RequireAuthorization(OrgPolicies.UploadCatalog)
            .WithRequestValidation()
            .WithName("CompleteCatalogReleaseCoverUpload")
            .WithSummary("Mark a cover upload as complete and attach it to the release.")
            .Produces<CompleteReleaseCoverUploadResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return endpoints;
    }
}
