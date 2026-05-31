using Amuse.Modules.Tenancy.Features.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Tenancy.Features.GetInvitePreview;

public static class GetInvitePreviewEndpoint
{
    public static RouteGroupBuilder MapGetInvitePreviewEndpoint(this RouteGroupBuilder group)
    {
        group.MapGet("/invites/{token}", async (
                string token,
                GetInvitePreviewHandler handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(token, cancellationToken);
                return result.ToTenancyResult(Results.Ok);
            })
            .AllowAnonymous()
            .WithName("GetOrganizationInvitePreview")
            .WithSummary("Preview an organization invite by token.")
            .Produces<InvitePreviewResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status410Gone);

        return group;
    }
}
