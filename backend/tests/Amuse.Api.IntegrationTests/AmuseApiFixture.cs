using Amuse.Modules.Catalog.Persistence;
using Amuse.Modules.Catalog.Processing;
using Amuse.Modules.Catalog.Seeding;
using Amuse.Modules.Common.Persistence;
using Amuse.Modules.Media;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;

namespace Amuse.Api.IntegrationTests;

public sealed class AmuseApiFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16")
        .WithDatabase("amuse_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private readonly InMemoryObjectStorage _objectStorage = new();

    private string? _connectionString;

    public InMemoryObjectStorage ObjectStorage => _objectStorage;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        _connectionString = _postgres.GetConnectionString();
        await ModuleDatabaseInitializer.MigrateAllAsync(Services);

        await using var scope = Services.CreateAsyncScope();
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

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddJsonFile(
                Path.Combine(AppContext.BaseDirectory, "appsettings.Testing.json"),
                optional: false,
                reloadOnChange: false);

            if (!string.IsNullOrWhiteSpace(_connectionString))
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = _connectionString,
                });
            }
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
        });
    }

    public HttpClient CreateClientWithCookies() =>
        CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            AllowAutoRedirect = false,
        });
}
