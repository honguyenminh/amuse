using Amuse.Modules.Common.Authorization;
using Amuse.Modules.Common.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Platform.Features.ListAccountingInvoices;

public static class ListAccountingInvoicesEndpoint
{
    public static RouteGroupBuilder MapListAccountingInvoicesEndpoint(this RouteGroupBuilder group)
    {
        group.MapGet("/accounting/invoices", async (
                ListAccountingInvoicesHandler handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(cancellationToken);
                return result.ToResult(Results.Ok);
            })
            .RequireAuthorization(PlatformPolicies.RequireAccountingRead)
            .WithName("ListPlatformAccountingInvoices")
            .WithSummary("List tax invoices for platform accounting.")
            .Produces<IReadOnlyList<PlatformTaxInvoiceRow>>();

        return group;
    }
}
