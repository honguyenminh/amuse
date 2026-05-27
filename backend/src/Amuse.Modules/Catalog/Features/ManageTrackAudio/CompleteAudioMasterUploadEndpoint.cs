using Amuse.Modules.Common.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Catalog.Features.ManageTrackAudio;

public static class CompleteAudioMasterUploadEndpoint
{
    public static IEndpointRouteBuilder MapCompleteAudioMasterUploadEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/v1/catalog/tracks/{trackId:guid}/audio-master/complete", async (
                Guid trackId,
                CompleteAudioMasterUploadRequest request,
                CompleteAudioMasterUploadHandler handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(trackId, request, cancellationToken);
                return result.ToResult(Results.Ok);
            })
            .RequireAuthorization()
            .WithRequestValidation()
            .WithName("CompleteCatalogTrackAudioMasterUpload")
            .WithSummary("Mark a master upload as complete and enqueue transcoding.")
            .Produces<CompleteAudioMasterUploadResponse>()
            .ProducesValidationProblem();

        return endpoints;
    }
}

