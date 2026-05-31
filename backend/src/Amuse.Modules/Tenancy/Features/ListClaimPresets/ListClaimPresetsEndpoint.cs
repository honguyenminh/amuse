using Amuse.Domain.Tenancy;
using Amuse.Modules.Tenancy.Features.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Tenancy.Features.ListClaimPresets;

public static class ListClaimPresetsEndpoint
{
    public static RouteGroupBuilder MapListClaimPresetsEndpoint(this RouteGroupBuilder group)
    {
        group.MapGet("/claim-presets", () =>
            {
                var presets = OrgClaimPresets.AllDefinitions
                    .Select(p => new ClaimPresetResponse(
                        p.Label,
                        p.DisplayName,
                        p.Description,
                        p.Icon,
                        p.Claims))
                    .ToList();
                return Results.Ok(presets);
            })
            .AllowAnonymous()
            .WithName("ListClaimPresets")
            .WithSummary("List preset role labels and their claim bundles.")
            .Produces<IReadOnlyList<ClaimPresetResponse>>(StatusCodes.Status200OK);

        return group;
    }
}
