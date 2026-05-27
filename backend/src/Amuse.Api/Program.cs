using System.Text.Json;
using System.Text.Json.Serialization;
using Amuse.Api;
using Amuse.Modules.Audit;
using Amuse.Modules.Catalog;
using Amuse.Modules.Catalog.Persistence;
using Amuse.Modules.Catalog.Seeding;
using Amuse.Modules.Catalog.Processing;
using Amuse.Modules.Common.Authorization;
using Amuse.Modules.Identity;
using Amuse.Modules.Listener;
using Amuse.Modules.Media;
using Amuse.Modules.Platform;
using Amuse.Modules.Platform.Persistence;
using Amuse.Modules.Platform.Seeding;
using Amuse.Modules.Tenancy;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
});

// TODO: configure OpenAPI and scalar
builder.Services.AddOpenApi();
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:3001")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});
builder.Services.AddIdentityModule(builder.Configuration);
builder.Services.AddTenancyModule(builder.Configuration);
builder.Services.AddListenerModule(builder.Configuration);
builder.Services.AddPlatformModule(builder.Configuration);
builder.Services.AddCatalogModule(builder.Configuration);
builder.Services.AddMediaModule(builder.Configuration);
builder.Services.AddAuditModule(builder.Configuration);

builder.Services.Configure<RabbitMqOptions>(
    builder.Configuration.GetSection("RabbitMq"));
builder.Services.AddSingleton<IAudioTranscodeJobQueue, RabbitMqAudioTranscodeJobQueue>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseCors("DevFrontend");

    // Dev-only idempotent seeding. NOT a migration: schema changes still live in EF migrations
    // applied by scripts/migrate-all.sh or the deploy pipeline.
    await using (var scope = app.Services.CreateAsyncScope())
    {
        var platformDb = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
        await PlatformRootSeeding.SeedAsync(platformDb, scope.ServiceProvider, CancellationToken.None);

        var catalogDb = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var objectStorage = scope.ServiceProvider.GetRequiredService<IObjectStorage>();
        await CatalogDevSeeding.SeedAsync(catalogDb, objectStorage, CancellationToken.None);
    }
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<TenantGuardMiddleware>();

app.MapIdentityModule();
app.MapListenerModule();
app.MapCatalogModule();
app.MapGet("/demo", () => "Hello, World!").RequireAuthorization();

app.Run();
