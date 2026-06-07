using System.Text;
using System.Text.Json;
using Amuse.Domain.Identity;
using Amuse.Modules.Common.Time;
using Amuse.Modules.Identity.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Amuse.Modules.Identity.Auth;

internal static class JwtBearerBlacklistExtensions
{
    public static IServiceCollection AddAmuseJwtAuthentication(
        this IServiceCollection services,
        JwtOptions jwt)
    {
        if (string.IsNullOrWhiteSpace(jwt.SigningKey))
            throw new InvalidOperationException("Jwt:SigningKey must be configured.");

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                    ClockSkew = TimeSpan.FromMinutes(1),
                };

                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        var jti = context.Principal?.FindFirst("jti")?.Value;
                        if (string.IsNullOrWhiteSpace(jti))
                            return Task.CompletedTask;

                        var blacklist = context.HttpContext.RequestServices.GetRequiredService<IJwtBlacklistStore>();
                        var clock = context.HttpContext.RequestServices.GetRequiredService<IClock>();

                        if (blacklist.IsRevoked(jti, clock.UtcNow))
                            context.Fail(IJwtBlacklistStore.RevokedFailureMessage);

                        return Task.CompletedTask;
                    },
                    OnChallenge = async context =>
                    {
                        if (context.AuthenticateFailure?.Message != IJwtBlacklistStore.RevokedFailureMessage)
                            return;

                        context.HandleResponse();
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.Response.ContentType = "application/problem+json";

                        await context.Response.WriteAsync(
                            JsonSerializer.Serialize(new ProblemDetails
                            {
                                Status = StatusCodes.Status401Unauthorized,
                                Title = IdentityErrors.TokenRevoked.Code,
                                Detail = IdentityErrors.TokenRevoked.Message,
                                Extensions = { ["code"] = IdentityErrors.TokenRevoked.Code },
                            }),
                            context.HttpContext.RequestAborted);
                    },
                };
            });

        services.AddAuthorization();
        return services;
    }
}
