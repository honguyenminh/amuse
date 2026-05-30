using Amuse.Domain.Tenancy;
using Amuse.Modules.Common.Authorization;
using Amuse.Modules.Common.Binding;
using Amuse.Modules.Common.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Platform.Features.ListOrganizationApplications;

public static class ListOrganizationApplicationsEndpoint
{
    public static RouteGroupBuilder MapListOrganizationApplicationsEndpoint(this RouteGroupBuilder group)
    {
        group.MapGet("/organizations/applications", async (
                string? status,
                ListOrganizationApplicationsHandler handler,
                CancellationToken cancellationToken) =>
            {
                if (!CamelCaseEnumQuery.TryParseOnboardingStatus(status, out var parsedStatus))
                {
                    return Results.Problem(
                        title: TenancyErrors.InvalidOnboardingStatusFilter.Code,
                        detail: TenancyErrors.InvalidOnboardingStatusFilter.Message,
                        statusCode: StatusCodes.Status400BadRequest,
                        extensions: new Dictionary<string, object?>
                        {
                            ["code"] = TenancyErrors.InvalidOnboardingStatusFilter.Code,
                        });
                }

                var result = await handler.HandleAsync(parsedStatus, cancellationToken);
                return result.ToResult(Results.Ok);
            })
            .RequireAuthorization(PlatformPolicies.RequireOrganizationReview)
            .WithName("ListOrganizationApplications")
            .WithSummary("List backing organizations awaiting platform review.")
            .Produces<IReadOnlyList<OrganizationApplicationResponse>>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return group;
    }
}
