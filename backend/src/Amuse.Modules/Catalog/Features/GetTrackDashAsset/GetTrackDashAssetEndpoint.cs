using Amuse.Modules.Common.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Catalog.Features.GetTrackDashAsset;

public static class GetTrackDashAssetEndpoint
{
    public static IEndpointRouteBuilder MapGetTrackDashAssetEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/catalog/tracks/{trackId:guid}/dash/{manifestId}/{assetName}", async (
                Guid trackId,
                string manifestId,
                string assetName,
                GetTrackDashAssetHandler handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(trackId, manifestId, assetName, cancellationToken);
                return result.ToResult(payload =>
                {
                    if (payload.RedirectUrl is not null)
                        return Results.Redirect(payload.RedirectUrl, permanent: false);
                    return Results.File(payload.Content.ToArray(), payload.ContentType);
                });
            })
            .RequireAuthorization()
            .WithName("GetCatalogTrackDashAsset")
            .WithSummary("Serve DASH manifest/segments through authenticated catalog endpoints.")
            .Produces(StatusCodes.Status302Found)
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return endpoints;
    }
}

