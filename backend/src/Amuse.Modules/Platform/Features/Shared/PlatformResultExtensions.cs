using Amuse.Domain.SharedKernel;
using Amuse.Domain.Tenancy;
using Microsoft.AspNetCore.Http;

namespace Amuse.Modules.Platform.Features.Shared;

internal static class PlatformResultExtensions
{
    public static IResult ToTenancyResult(this Result result) =>
        result.IsSuccess
            ? Results.NoContent()
            : ToProblem(result.Error!);

    private static IResult ToProblem(DomainError error)
    {
        var statusCode = error.Code switch
        {
            _ when error.Code == TenancyErrors.OrganizationNotFound.Code => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status400BadRequest,
        };

        return Results.Problem(
            title: error.Code,
            detail: error.Message,
            statusCode: statusCode,
            extensions: new Dictionary<string, object?> { ["code"] = error.Code });
    }
}
