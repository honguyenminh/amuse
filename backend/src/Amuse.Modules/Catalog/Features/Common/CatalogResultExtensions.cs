using Amuse.Domain.Catalog;
using Amuse.Domain.SharedKernel;
using Microsoft.AspNetCore.Http;

namespace Amuse.Modules.Catalog.Features.Common;

internal static class CatalogResultExtensions
{
    public static IResult ToCatalogResult<T>(this Result<T> result, Func<T, IResult> onSuccess) =>
        result.IsSuccess
            ? onSuccess(result.Value!)
            : ToProblem(result.Error!);

    private static IResult ToProblem(DomainError error)
    {
        var statusCode = error.Code switch
        {
            _ when error.Code == CatalogErrors.StreamPlaybackForbidden.Code
                => StatusCodes.Status403Forbidden,
            _ when error.Code == CatalogErrors.TrackNotFound.Code
                => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status400BadRequest,
        };

        return Results.Problem(
            title: error.Code,
            detail: error.Message,
            statusCode: statusCode,
            extensions: new Dictionary<string, object?> { ["code"] = error.Code });
    }
}
