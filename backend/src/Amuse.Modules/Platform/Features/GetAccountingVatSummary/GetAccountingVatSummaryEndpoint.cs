using Amuse.Modules.Common.Authorization;
using Amuse.Modules.Common.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Platform.Features.GetAccountingVatSummary;

public static class GetAccountingVatSummaryEndpoint
{
    public static RouteGroupBuilder MapGetAccountingVatSummaryEndpoint(this RouteGroupBuilder group)
    {
        group.MapGet("/accounting/vat-summary", async (
                DateTimeOffset? from,
                DateTimeOffset? to,
                GetAccountingVatSummaryHandler handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(from, to, cancellationToken);
                return result.ToResult(Results.Ok);
            })
            .RequireAuthorization(PlatformPolicies.RequireAccountingRead)
            .WithName("GetPlatformAccountingVatSummary")
            .WithSummary("VAT invoiced vs credited and ledger VatPayable movement for a period.")
            .Produces<PlatformVatSummaryResponse>();

        return group;
    }
}
