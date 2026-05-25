using Amuse.Modules.Common.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Catalog.Features.GetAlbumDetail;

public static class GetAlbumDetailEndpoint
{
    public static IEndpointRouteBuilder MapGetAlbumDetailEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/catalog/albums/{albumId:guid}", async (
                Guid albumId,
                GetAlbumDetailHandler handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(albumId, cancellationToken);
                return result.ToResult(Results.Ok);
            })
            .RequireAuthorization()
            .WithName("GetCatalogAlbumDetail")
            .WithSummary("Return an album with its tracks. Returns problem `catalog.album_not_found` if missing.")
            .Produces<GetAlbumDetailResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return endpoints;
    }
}
