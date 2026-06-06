using Amuse.Modules.Catalog.Persistence;
using Amuse.Modules.Catalog.Processing;
using Amuse.Modules.Catalog.Seeding;
using Amuse.Modules.Common.Persistence;
using Amuse.Modules.Identity.Email;
using Amuse.Modules.Media;
using Amuse.Modules.Platform.Persistence;
using Amuse.Modules.Platform.Seeding;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Testcontainers.PostgreSql;

namespace Amuse.Api.IntegrationTests;

public sealed class AmuseApiFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string DevDatabaseName = "amuse_development";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16")
        .WithDatabase("amuse_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private readonly InMemoryObjectStorage _objectStorage = new();
    private readonly CaptureEmailSender _captureEmailSender = new();

    private string? _connectionString;

    public InMemoryObjectStorage ObjectStorage => _objectStorage;

    public CaptureEmailSender CaptureEmailSender => _captureEmailSender;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        _connectionString = _postgres.GetConnectionString();
        AssertIsolatedDatabase(_connectionString);

        await ModuleDatabaseInitializer.MigrateAllAsync(Services);

        await using var scope = Services.CreateAsyncScope();
        var platformDb = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
        await PlatformRootSeeding.SeedAsync(platformDb, scope.ServiceProvider, CancellationToken.None);

        var catalogDb = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var storage = scope.ServiceProvider.GetRequiredService<IObjectStorage>();
        var jobQueue = scope.ServiceProvider.GetRequiredService<IAudioTranscodeJobQueue>();
        var clock = scope.ServiceProvider.GetRequiredService<Amuse.Modules.Common.Time.IClock>();
        await CatalogDevSeeding.SeedAsync(catalogDb, storage, jobQueue, clock, CancellationToken.None);
    }

    public override async ValueTask DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await base.DisposeAsync();
    }

    Task IAsyncLifetime.DisposeAsync() => DisposeAsync().AsTask();

    protected override IHost CreateHost(IHostBuilder builder)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Integration test host cannot start before PostgreSQL Testcontainer is running.");
        }

        AssertIsolatedDatabase(_connectionString);
        return base.CreateHost(builder);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Do not use Development here: that loads appsettings.Development.json and the local
        // amuse_development connection string, which pollutes the dev database when config override fails.
        builder.UseEnvironment("Testing");

        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Connection string must be set before the test host is configured.");
        }

        AssertIsolatedDatabase(_connectionString);

        var testingSettingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.Testing.json");
        var overrides = new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = _connectionString,
        };

        // Minimal hosting reads configuration during Program startup; provide overrides
        // both before and after WebApplication.CreateBuilder(args) runs.
        var earlyConfig = new ConfigurationBuilder()
            .AddJsonFile(testingSettingsPath, optional: false, reloadOnChange: false)
            .AddInMemoryCollection(overrides)
            .Build();

        builder
            .UseConfiguration(earlyConfig)
            .ConfigureAppConfiguration((_, config) =>
            {
                config.AddJsonFile(testingSettingsPath, optional: false, reloadOnChange: false);
                config.AddInMemoryCollection(overrides);
            });

        builder.ConfigureServices(services =>
        {
            // Replace the real S3-backed storage with the in-memory fake so tests
            // don't depend on a running MinIO. Both the AWS S3 client and the storage
            // service registrations must be cleared, since the real client tries to
            // contact MinIO on first call even if storage itself is replaced.
            services.RemoveAll<Amazon.S3.IAmazonS3>();
            services.RemoveAll<IObjectStorage>();
            services.AddSingleton<IObjectStorage>(_objectStorage);

            services.RemoveAll<IAudioTranscodeJobQueue>();
            services.AddSingleton<IAudioTranscodeJobQueue, InMemoryAudioTranscodeJobQueue>();

            services.RemoveAll<IEmailSender>();
            services.AddSingleton(_captureEmailSender);
            services.AddSingleton<IEmailSender>(_captureEmailSender);
        });
    }

    public HttpClient CreateClientWithCookies() =>
        CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            AllowAutoRedirect = false,
        });

    private static void AssertIsolatedDatabase(string connectionString)
    {
        if (connectionString.Contains(DevDatabaseName, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Integration tests must not use the local development database ({DevDatabaseName}). " +
                "Ensure Docker is available for Testcontainers and that no environment variable " +
                "overrides ConnectionStrings__DefaultConnection to the dev instance.");
        }
    }
}
