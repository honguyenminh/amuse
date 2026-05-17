using Amuse.Modules.Common.Authorization;
using Amuse.Modules.Common.Endpoints;
using Amuse.Modules.Common.Time;
using FluentValidation;
using Amuse.Modules.Identity.Features.ExternalLoginComplete;
using Amuse.Modules.Identity.Features.GetCurrentAccount;
using Amuse.Modules.Identity.Features.ListAvailablePersonas;
using Amuse.Modules.Identity.Features.LoginPassword;
using Amuse.Modules.Identity.Features.RefreshToken;
using Amuse.Modules.Identity.Features.RevokeToken;
using Amuse.Modules.Identity.Options;
using Amuse.Modules.Identity.Persistence;
using Amuse.Modules.Identity.Auth;
using Amuse.Modules.Identity.Auth.External;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
namespace Amuse.Modules.Identity;

public static class IdentityModule
{
    public static IServiceCollection AddIdentityModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' is not configured.");

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<ExternalProviderOptions>(configuration.GetSection(ExternalProviderOptions.SectionName));

        services.AddDbContext<IdentityDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory_identity", "identity")));

        services
            .AddIdentity<ApplicationUser, IdentityRole<Guid>>(identity =>
            {
                identity.User.RequireUniqueEmail = true;
                identity.Password.RequireDigit = true;
                identity.Password.RequiredLength = 10;
            })
            .AddEntityFrameworkStores<IdentityDbContext>()
            .AddDefaultTokenProviders();

        services.AddHttpClient();
        services.AddValidatorsFromAssemblyContaining<LoginPasswordRequestValidator>();
        services.AddPersonaAuthorization();
        services.AddSingleton<IClock, SystemClock>();

        services.AddScoped<AccountLinker>();
        services.AddScoped<TokenIssuer>();
        services.AddScoped<ExternalIdentityResolverFactory>();

        services.AddScoped<LoginPasswordHandler>();
        services.AddScoped<ExternalLoginCompleteHandler>();
        services.AddScoped<RefreshTokenHandler>();
        services.AddScoped<RevokeTokenHandler>();
        services.AddScoped<ListAvailablePersonasHandler>();
        services.AddScoped<GetCurrentAccountHandler>();

        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? new JwtOptions();
        if (string.IsNullOrWhiteSpace(jwt.SigningKey))
            throw new InvalidOperationException("Jwt:SigningKey must be configured.");

        services.AddAmuseJwtAuthentication(jwt);
        return services;
    }

    public static IEndpointRouteBuilder MapIdentityModule(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/identity");

        group.MapLoginPasswordEndpoint();
        group.MapExternalLoginCompleteEndpoint();
        group.MapRefreshTokenEndpoint();
        group.MapRevokeTokenEndpoint();
        group.MapGetCurrentAccountEndpoint();
        group.MapListAvailablePersonasEndpoint();

        return endpoints;
    }
}
