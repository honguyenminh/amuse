using Amuse.Modules.Catalog.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Amuse.Api.IntegrationTests;

internal static class CatalogOutboxTestSupport
{
    public static async Task DrainPendingAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var scopeFactory = services.GetRequiredService<IServiceScopeFactory>();
        var logger = services.GetRequiredService<ILogger<CatalogOutboxProcessor>>();
        var processor = new CatalogOutboxProcessor(scopeFactory, logger);
        await processor.ProcessBatchAsync(cancellationToken);
    }
}
