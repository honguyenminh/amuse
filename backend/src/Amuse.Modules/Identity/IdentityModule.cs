using Amuse.Modules.Common.Authorization;
using Amuse.Modules.Common.Endpoints;
using Amuse.Modules.Common.Time;
using FluentValidation;
using Amuse.Modules.Identity.Email;
using Amuse.Modules.Identity.Features.ConfirmEmail;
using Amuse.Modules.Identity.Features.ExternalLoginComplete;
using Amuse.Modules.Identity.Features.GetCurrentAccount;
using Amuse.Modules.Identity.Features.ListAvailablePersonas;
using Amuse.Modules.Identity.Features.LoginPassword;
using Amuse.Modules.Identity.Features.RefreshToken;
using Amuse.Modules.Identity.Features.RegisterPassword;
using Amuse.Modules.Identity.Features.ResendConfirmation;
using Amuse.Modules.Identity.Features.RevokeToken;
using Amuse.Modules.Identity.Options;
using Amuse.Modules.Identity.Persistence;
using Amuse.Modules.Identity.Services;
using Amuse.Modules.Tenancy.Contracts;
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
        services.Configure<IdentityEmailOptions>(configuration.GetSection(IdentityEmailOptions.SectionName));

        services.AddDbContext<IdentityDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory_identity", "identity")));

        // AddIdentityCore (not AddIdentity) avoids registering cookie authentication and
        // overriding the default challenge scheme. JWT bearer (see AddAmuseJwtAuthentication
        // below) is the only authentication scheme for this API.
        services
            .AddIdentityCore<ApplicationUser>(identity =>
            {
                identity.User.RequireUniqueEmail = true;
                identity.Password.RequireDigit = true;
                identity.Password.RequiredLength = 10;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<IdentityDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        services.AddHttpClient();
        services.AddValidatorsFromAssemblyContaining<LoginPasswordRequestValidator>();
        services.AddPersonaAuthorization();
        services.AddSingleton<IClock, SystemClock>();

        services.AddScoped<AccountLinker>();
        services.AddScoped<IOrganizationCreatorContactLookup, OrganizationCreatorContactLookup>();
        services.AddScoped<TokenIssuer>();
        services.AddScoped<ExternalIdentityResolverFactory>();
        RegisterEmailSender(services, configuration);
        services.AddScoped<EmailConfirmationLinkBuilder>();
        services.AddSingleton<ConfirmationResendThrottle>();

        services.AddScoped<LoginPasswordHandler>();
        services.AddScoped<RegisterPasswordHandler>();
        services.AddScoped<ConfirmEmailHandler>();
        services.AddScoped<ResendConfirmationHandler>();
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

    private static void RegisterEmailSender(
        IServiceCollection services,
        IConfiguration configuration)
    {
        var email = configuration.GetSection(IdentityEmailOptions.SectionName).Get<IdentityEmailOptions>()
            ?? new IdentityEmailOptions();

        if (email.Smtp.Enabled && !string.IsNullOrWhiteSpace(email.Smtp.Host))
            services.AddSingleton<IEmailSender, SmtpEmailSender>();
        else
            services.AddSingleton<IEmailSender, LogEmailSender>();
    }

    public static IEndpointRouteBuilder MapIdentityModule(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/identity");

        group.MapLoginPasswordEndpoint();
        group.MapRegisterPasswordEndpoint();
        group.MapConfirmEmailEndpoint();
        group.MapResendConfirmationEndpoint();
        group.MapExternalLoginCompleteEndpoint();
        group.MapRefreshTokenEndpoint();
        group.MapRevokeTokenEndpoint();
        group.MapGetCurrentAccountEndpoint();
        group.MapListAvailablePersonasEndpoint();

        return endpoints;
    }
}
