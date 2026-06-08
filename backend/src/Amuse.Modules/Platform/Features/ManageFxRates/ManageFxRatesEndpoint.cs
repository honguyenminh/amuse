using Amuse.Modules.Common.Authorization;
using Amuse.Modules.Common.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Platform.Features.ManageFxRates;

public static class ManageFxRatesEndpoint
{
    public static RouteGroupBuilder MapManageFxRatesEndpoint(this RouteGroupBuilder group)
    {
        group.MapGet("/accounting/fx-rates", async (
                string? quoteCurrency,
                ListFxRatesHandler handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(quoteCurrency, cancellationToken);
                return result.ToResult(Results.Ok);
            })
            .RequireAuthorization(PlatformPolicies.RequireAccountingRead)
            .WithName("ListPlatformFxRates")
            .WithSummary("List recent USD cross FX rates (ECB daily and ops overrides).")
            .Produces<IReadOnlyList<PlatformFxRateRow>>();

        group.MapPost("/accounting/fx-rates", async (
                PublishFxRateOverrideRequest request,
                PublishFxRateOverrideHandler handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(request, cancellationToken);
                return result.ToResult(Results.Ok);
            })
            .RequireAuthorization(PlatformPolicies.RequireAccountingManage)
            .WithRequestValidation()
            .WithName("PublishFxRateOverride")
            .WithSummary(
                "Publish an ops manual FX override (USD/quote). Requires manage:platform:accounting:all. Returns billing.fx_rate.invalid when rate or currency is invalid.")
            .Produces<PlatformFxRateRow>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return group;
    }
}
