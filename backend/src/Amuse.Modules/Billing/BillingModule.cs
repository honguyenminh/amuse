using Amuse.Domain.Billing;
using Amuse.Modules.Billing.Contracts;
using Amuse.Modules.Billing.Features.AcquireFree;
using Amuse.Modules.Billing.Features.Balance;
using Amuse.Modules.Billing.Features.CheckOwnership;
using Amuse.Modules.Billing.Features.CreateCheckoutSession;
using Amuse.Modules.Billing.Features.DownloadTrack;
using Amuse.Modules.Billing.Features.Common;
using Amuse.Modules.Billing.Features.ListMyPurchases;
using Amuse.Modules.Billing.Features.PayoutProfile;
using Amuse.Modules.Billing.Features.Statements;
using Amuse.Modules.Billing.Features.Withdrawals;
using Amuse.Modules.Billing.Features.RefundPurchase;
using Amuse.Modules.Billing.Features.StripeWebhook;
using Amuse.Modules.Billing.Persistence;
using Amuse.Modules.Billing.Services;
using Amuse.Modules.Common.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Amuse.Modules.Billing;

public static class BillingModule
{
    public static IServiceCollection AddBillingModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.Configure<PlatformFeeConfig>(configuration.GetSection("Billing:PlatformFee"));
        services.Configure<TaxConfig>(configuration.GetSection("Billing:Tax"));
        services.Configure<WithdrawalAutoApproveConfig>(configuration.GetSection("Billing:WithdrawalAutoApprove"));
        services.Configure<HoldConfig>(configuration.GetSection("Billing:Hold"));
        services.Configure<StripeConfig>(configuration.GetSection(StripeConfig.SectionName));
        services.Configure<CheckoutConfig>(configuration.GetSection(CheckoutConfig.SectionName));
        services.Configure<GlobalPayoutConfig>(configuration.GetSection(GlobalPayoutConfig.SectionName));
        services.Configure<FxRateImportConfig>(configuration.GetSection(FxRateImportConfig.SectionName));
        services.Configure<BillingSchedulerOptions>(configuration.GetSection(BillingSchedulerOptions.SectionName));

        services.AddHttpClient(nameof(EcbFxRateImporter));

        services.AddModulePersistenceInfrastructure();
        services.TryAddSingleton(_ => new AuditEntityRegistry());

        services.AddDbContext<BillingDbContext>((sp, options) =>
        {
            BillingDbContextOptions.Configure(
                (DbContextOptionsBuilder<BillingDbContext>)options,
                connectionString);
            options.AddModuleInterceptors(sp);
        });

        services.AddScoped<IFxRateReadModel, FxRateReadModel>();
        services.AddScoped<ILedgerBalanceReadModel, LedgerBalanceReadModel>();
        services.AddScoped<EcbFxRateImporter>();

        return services;
    }

    /// <summary>
    /// HTTP handlers and Stripe webhook pipeline. Requires identity, audit, and catalog modules.
    /// Do not register this in worker hosts that only run billing background jobs.
    /// </summary>
    public static IServiceCollection AddBillingHandlers(this IServiceCollection services)
    {
        services.AddScoped<IEntitlementReadModel, EntitlementReadModel>();
        services.AddScoped<ICheckoutProvider, StripeCheckoutProvider>();
        services.AddScoped<IGlobalPayoutProvider, StripeGlobalPayoutProvider>();
        services.AddScoped<StripeWithdrawalExecutionService>();
        services.AddScoped<PaidPurchaseCompletionService>();
        services.AddScoped<RefundCompletionService>();
        services.AddScoped<ChargebackCompletionService>();
        services.AddScoped<ISensitiveFieldProtector, SensitiveFieldProtector>();
        services.AddScoped<AcquireFreeHandler>();
        services.AddScoped<ListMyPurchasesHandler>();
        services.AddScoped<CheckOwnershipHandler>();
        services.AddScoped<DownloadTrackHandler>();
        services.AddScoped<CreateCheckoutSessionHandler>();
        services.AddScoped<StripeWebhookHandler>();
        services.AddScoped<GetPayoutProfileHandler>();
        services.AddScoped<UpsertPayoutProfileHandler>();
        services.AddScoped<SubmitPayoutProfileHandler>();
        services.AddScoped<CreateStripeAccountLinkHandler>();
        services.AddScoped<GetBalanceHandler>();
        services.AddScoped<ListStatementsHandler>();
        services.AddScoped<CreateWithdrawalHandler>();
        services.AddScoped<ListWithdrawalsHandler>();
        services.AddScoped<RefundPurchaseHandler>();

        services.AddValidatorsFromAssemblyContaining<FreeAcquisitionRequestValidator>();

        services.AddDataProtection();

        return services;
    }

    public static IServiceCollection AddBillingSchedulerWorkers(this IServiceCollection services)
    {
        services.AddHostedService<PendingToAvailableWorker>();
        services.AddHostedService<FxRateImportWorker>();
        return services;
    }

    public static IEndpointRouteBuilder MapBillingModule(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapAcquireFreeEndpoint();
        endpoints.MapListMyPurchasesEndpoint();
        endpoints.MapCheckOwnershipEndpoint();
        endpoints.MapDownloadTrackEndpoint();
        endpoints.MapCreateCheckoutSessionEndpoint();
        endpoints.MapStripeWebhookEndpoint();
        endpoints.MapPayoutProfileEndpoint();
        endpoints.MapGetBalanceEndpoint();
        endpoints.MapListStatementsEndpoint();
        endpoints.MapWithdrawalsEndpoint();
        endpoints.MapRefundPurchaseEndpoint();
        return endpoints;
    }
}
