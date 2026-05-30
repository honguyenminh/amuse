using Amuse.Modules.Common.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Amuse.Modules.Identity.Features.RegisterPassword;

public static class RegisterPasswordEndpoint
{
    public static RouteGroupBuilder MapRegisterPasswordEndpoint(this RouteGroupBuilder group)
    {
        group.MapPost("/register/password", async (
                RegisterPasswordRequest request,
                RegisterPasswordHandler handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(request, cancellationToken);
                return result.ToResult(body => Results.Json(body, statusCode: StatusCodes.Status202Accepted));
            })
            .WithRequestValidation()
            .WithName("RegisterPassword")
            .WithSummary("Register a new account with email and password.")
            .Produces<RegisterPasswordResponse>(StatusCodes.Status202Accepted)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesValidationProblem();

        return group;
    }
}
