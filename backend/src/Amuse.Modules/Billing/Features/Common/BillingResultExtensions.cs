using Amuse.Domain.Billing;
using Amuse.Domain.SharedKernel;
using Microsoft.AspNetCore.Http;

namespace Amuse.Modules.Billing.Features.Common;

internal static class BillingResultExtensions
{
    public static IResult ToBillingResult(this Result result) =>
        result.IsSuccess
            ? Results.NoContent()
            : ToProblem(result.Error!);

    public static IResult ToBillingResult(this Result result, Func<IResult> onSuccess) =>
        result.IsSuccess
            ? onSuccess()
            : ToProblem(result.Error!);

    public static IResult ToBillingResult<T>(this Result<T> result, Func<T, IResult> onSuccess) =>
        result.IsSuccess
            ? onSuccess(result.Value!)
            : ToProblem(result.Error!);

    private static IResult ToProblem(DomainError error)
    {
        var statusCode = error.Code switch
        {
            _ when error.Code == BillingErrors.PurchaseNotFound.Code
                || error.Code == BillingErrors.TrackNotFound.Code
                || error.Code == BillingErrors.ReleaseNotFound.Code
                || error.Code == BillingErrors.PayoutProfileNotFound.Code
                || error.Code == BillingErrors.WithdrawalNotFound.Code
                || error.Code == BillingErrors.TaxInvoiceNotFound.Code => StatusCodes.Status404NotFound,
            _ when error.Code == Amuse.Domain.Identity.IdentityErrors.InvalidRefreshToken.Code
                || error.Code == Amuse.Domain.Identity.IdentityErrors.AccountBanned.Code
                || error.Code == BillingErrors.AccountBanned.Code
                || error.Code == BillingErrors.DownloadForbidden.Code
                || error.Code == BillingErrors.RefundNotAllowed.Code
                || error.Code == Amuse.Domain.Identity.IdentityErrors.InvalidPersonaContext.Code => StatusCodes.Status403Forbidden,
            _ when error.Code == BillingErrors.DownloadNotReady.Code => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status400BadRequest,
        };

        return Results.Problem(
            title: error.Code,
            detail: error.Message,
            statusCode: statusCode,
            extensions: new Dictionary<string, object?> { ["code"] = error.Code });
    }
}
