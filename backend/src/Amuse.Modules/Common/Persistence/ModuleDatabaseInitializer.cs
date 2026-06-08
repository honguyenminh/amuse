using Amuse.Modules.Audit.Persistence;
using Amuse.Modules.Billing.Persistence;
using Amuse.Modules.Catalog.Persistence;
using Amuse.Modules.Discovery.Persistence;
using Amuse.Modules.Identity.Persistence;
using Amuse.Modules.Ingestion.Persistence;
using Amuse.Modules.Listener.Persistence;
using Amuse.Modules.Platform.Persistence;
using Amuse.Modules.Tenancy.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Amuse.Modules.Common.Persistence;

public static class ModuleDatabaseInitializer
{
    public static async Task MigrateAllAsync(
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        await MigrateAsync<IdentityDbContext>(serviceProvider, cancellationToken);
        await MigrateAsync<TenancyDbContext>(serviceProvider, cancellationToken);
        await MigrateAsync<ListenerDbContext>(serviceProvider, cancellationToken);
        await MigrateAsync<PlatformDbContext>(serviceProvider, cancellationToken);
        await MigrateAsync<CatalogDbContext>(serviceProvider, cancellationToken);
        await MigrateAsync<DiscoveryDbContext>(serviceProvider, cancellationToken);
        await MigrateAsync<IngestionDbContext>(serviceProvider, cancellationToken);
        await MigrateAsync<BillingDbContext>(serviceProvider, cancellationToken);
        await MigrateAsync<AuditDbContext>(serviceProvider, cancellationToken);
    }

    private static async Task MigrateAsync<TContext>(
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
        where TContext : DbContext
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<TContext>();
        await context.Database.MigrateAsync(cancellationToken);
    }
}
