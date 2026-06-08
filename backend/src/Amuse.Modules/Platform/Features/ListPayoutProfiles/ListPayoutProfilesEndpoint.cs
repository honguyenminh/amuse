using Amuse.Domain.Billing;
using Amuse.Modules.Billing.Features.PayoutProfile;
using Amuse.Modules.Common.Authorization;
using Amuse.Modules.Common.Binding;
using Amuse.Modules.Common.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Platform.Features.ListPayoutProfiles;

public static class ListPayoutProfilesEndpoint
{
    public static RouteGroupBuilder MapListPayoutProfilesEndpoint(this RouteGroupBuilder group)
    {
        group.MapGet("/payout-profiles", async (
                string? status,
                ListPayoutProfilesHandler handler,
                CancellationToken cancellationToken) =>
            {
                if (!CamelCaseEnumQuery.TryParsePayoutVerificationStatus(status, out var parsedStatus))
                {
                    return Results.Problem(
                        title: BillingErrors.InvalidPayoutVerificationStatusFilter.Code,
                        detail: BillingErrors.InvalidPayoutVerificationStatusFilter.Message,
                        statusCode: StatusCodes.Status400BadRequest,
                        extensions: new Dictionary<string, object?>
                        {
                            ["code"] = BillingErrors.InvalidPayoutVerificationStatusFilter.Code,
                        });
                }

                var result = await handler.HandleAsync(parsedStatus, cancellationToken);
                return result.ToResult(Results.Ok);
            })
            .RequireAuthorization(PlatformPolicies.RequirePayoutManage)
            .WithName("ListPlatformPayoutProfiles")
            .WithSummary("List payout profiles awaiting platform review.")
            .Produces<IReadOnlyList<PlatformPayoutProfileRow>>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return group;
    }
}
