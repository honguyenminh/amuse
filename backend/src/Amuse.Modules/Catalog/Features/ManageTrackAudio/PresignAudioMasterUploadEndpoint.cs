using Amuse.Modules.Common.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Catalog.Features.ManageTrackAudio;

public static class PresignAudioMasterUploadEndpoint
{
    public static IEndpointRouteBuilder MapPresignAudioMasterUploadEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/v1/catalog/tracks/{trackId:guid}/audio-master/presign-upload", async (
                Guid trackId,
                PresignAudioMasterUploadRequest request,
                PresignAudioMasterUploadHandler handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(trackId, request, cancellationToken);
                return result.ToResult(Results.Ok);
            })
            .RequireAuthorization()
            .WithRequestValidation()
            .WithName("PresignCatalogTrackAudioMasterUpload")
            .WithSummary("Return a short-lived presigned PUT URL for uploading a track master audio file.")
            .Produces<PresignAudioMasterUploadResponse>()
            .ProducesValidationProblem();

        return endpoints;
    }
}

