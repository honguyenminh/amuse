using Amuse.Modules.Catalog.Persistence;
using Amuse.Modules.Catalog.Seeding;
using Amuse.Modules.Common.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

    private string? _connectionString;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        _connectionString = _postgres.GetConnectionString();
        await ModuleDatabaseInitializer.MigrateAllAsync(Services);

        await using var scope = Services.CreateAsyncScope();
        var catalogDb = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        await CatalogDevSeeding.SeedAsync(catalogDb, CancellationToken.None);
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
    }

    public HttpClient CreateClientWithCookies() =>
        CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            AllowAutoRedirect = false,
        });
}
