using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Amuse.Modules.Common.Endpoints;

public static class ValidatedRouteHandlerBuilderExtensions
{
    public static RouteHandlerBuilder WithRequestValidation(this RouteHandlerBuilder builder) =>
        builder.AddEndpointFilter<RequestValidationFilter>();
}
