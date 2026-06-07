using Amuse.Domain.SharedKernel;
using Amuse.Domain.Tenancy;
using Microsoft.AspNetCore.Http;

namespace Amuse.Modules.Tenancy.Features.Common;

internal static class TenancyResultExtensions
{
    public static IResult ToTenancyResult<T>(this Result<T> result, Func<T, IResult> onSuccess) =>
        result.IsSuccess
            ? onSuccess(result.Value!)
            : ToProblem(result.Error!);

    public static IResult ToTenancyResult(this Result result) =>
        result.IsSuccess
            ? Results.NoContent()
            : ToProblem(result.Error!);

    private static IResult ToProblem(DomainError error)
    {
        var statusCode = error.Code switch
        {
            _ when error.Code == TenancyErrors.OrganizationNotFound.Code => StatusCodes.Status404NotFound,
            _ when error.Code == TenancyErrors.NotOrganizationMember.Code => StatusCodes.Status404NotFound,
            _ when error.Code == TenancyErrors.MemberNotFound.Code => StatusCodes.Status404NotFound,
            _ when error.Code == TenancyErrors.InviteNotFound.Code => StatusCodes.Status404NotFound,
            _ when error.Code == TenancyErrors.InsufficientClaim.Code => StatusCodes.Status403Forbidden,
            _ when error.Code == TenancyErrors.InviteEmailMismatch.Code => StatusCodes.Status403Forbidden,
            _ when error.Code == TenancyErrors.DuplicateMember.Code => StatusCodes.Status409Conflict,
            _ when error.Code == TenancyErrors.DuplicatePendingInvite.Code => StatusCodes.Status409Conflict,
            _ when error.Code == TenancyErrors.InviteExpired.Code => StatusCodes.Status410Gone,
            _ => StatusCodes.Status400BadRequest,
        };

        return Results.Problem(
            title: error.Code,
            detail: error.Message,
            statusCode: statusCode,
            extensions: new Dictionary<string, object?> { ["code"] = error.Code });
    }
}
