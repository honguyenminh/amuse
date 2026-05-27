using Amuse.Modules.Common.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Catalog.Features.GetArtistDetail;

public static class GetArtistDetailEndpoint
{
    public static IEndpointRouteBuilder MapGetArtistDetailEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/catalog/artists/{artistId:guid}", async (
                Guid artistId,
                GetArtistDetailHandler handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(artistId, cancellationToken);
                return result.ToResult(Results.Ok);
            })
            .AllowAnonymous()
            .WithName("GetCatalogArtistDetail")
            .WithSummary("Return an artist with their releases. Public; no authentication required. Returns problem `catalog.artist_not_found` if missing.")
            .Produces<GetArtistDetailResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return endpoints;
    }
}
