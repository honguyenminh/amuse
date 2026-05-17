using Amuse.Domain.SharedKernel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Amuse.Modules.Common.Endpoints;

public static class ProblemDetailsMappingExtensions
{
    public static IResult ToResult(this Result result) =>
        result.IsSuccess
            ? Results.NoContent()
            : Results.Problem(
                title: result.Error!.Code,
                detail: result.Error.Message,
                statusCode: StatusCodes.Status400BadRequest,
                extensions: new Dictionary<string, object?> { ["code"] = result.Error.Code });

    public static IResult ToResult<T>(this Result<T> result, Func<T, IResult> onSuccess) =>
        result.IsSuccess
            ? onSuccess(result.Value!)
            : Results.Problem(
                title: result.Error!.Code,
                detail: result.Error.Message,
                statusCode: StatusCodes.Status400BadRequest,
                extensions: new Dictionary<string, object?> { ["code"] = result.Error.Code });
}
