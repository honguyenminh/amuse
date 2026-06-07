using Amuse.Domain.Discovery;
using Amuse.Domain.SharedKernel;
using Microsoft.AspNetCore.Http;

namespace Amuse.Modules.Discovery.Features.Common;

internal static class DiscoveryResultExtensions
{
    public static IResult ToDiscoveryResult(this Result result) =>
        result.IsSuccess
            ? Results.NoContent()
            : ToProblem(result.Error!);

    public static IResult ToDiscoveryResult<T>(this Result<T> result, Func<T, IResult> onSuccess) =>
        result.IsSuccess
            ? onSuccess(result.Value!)
            : ToProblem(result.Error!);

    private static IResult ToProblem(DomainError error)
    {
        var statusCode = error.Code switch
        {
            _ when error.Code == DiscoveryErrors.PlaylistNotFound.Code
                || error.Code == DiscoveryErrors.LibraryEntryNotFound.Code
                || error.Code == DiscoveryErrors.LikedTrackNotFound.Code => StatusCodes.Status404NotFound,
            _ when error.Code == DiscoveryErrors.PlaylistForbidden.Code => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status400BadRequest,
        };

        return Results.Problem(
            title: error.Code,
            detail: error.Message,
            statusCode: statusCode,
            extensions: new Dictionary<string, object?> { ["code"] = error.Code });
    }
}
