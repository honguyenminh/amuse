using Amuse.Domain.Identity;
using Amuse.Domain.Platform;
using Amuse.Modules.Identity.Persistence;
using Amuse.Modules.Platform.Options;
using Amuse.Modules.Platform.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Amuse.Modules.Platform.Seeding;

public static class PlatformRootSeeding
{
    public static async Task SeedAsync(
        PlatformDbContext platformDb,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        if (await platformDb.PlatformOperators.AnyAsync(o => o.Id == PlatformOperatorId.Root, cancellationToken))
            return;

        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        var rootOptions = configuration.GetSection(PlatformRootOptions.SectionName).Get<PlatformRootOptions>()
            ?? throw new InvalidOperationException(
                $"Configuration section '{PlatformRootOptions.SectionName}' is required for platform root seeding.");

        if (rootOptions.AccountId == Guid.Empty)
            throw new InvalidOperationException(
                $"{PlatformRootOptions.SectionName}:AccountId must be a non-empty GUID.");

        await IdentityRootSeeding.EnsureRootAccountAsync(
            serviceProvider,
            rootOptions,
            cancellationToken);

        platformDb.PlatformOperators.Add(PlatformOperator.Create(
            PlatformOperatorId.Root,
            AccountId.From(rootOptions.AccountId),
            rootOptions.Claims,
            DateTimeOffset.UtcNow));

        await platformDb.SaveChangesAsync(cancellationToken);

        await platformDb.Database.ExecuteSqlRawAsync(
            """
            SELECT setval(
                pg_get_serial_sequence('platform.platform_operator', 'id'),
                GREATEST((SELECT COALESCE(MAX(id), 1) FROM platform.platform_operator), 1));
            """,
            cancellationToken);
    }
}
