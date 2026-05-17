using System.Text.Json;
using System.Text.Json.Serialization;
using Amuse.Modules.Audit;
using Amuse.Modules.Common.Authorization;
using Amuse.Modules.Identity;
using Amuse.Modules.Listener;
using Amuse.Modules.Platform;
using Amuse.Modules.Tenancy;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
});

// TODO: configure OpenAPI and scalar
builder.Services.AddOpenApi();
builder.Services.AddIdentityModule(builder.Configuration);
builder.Services.AddTenancyModule(builder.Configuration);
builder.Services.AddListenerModule(builder.Configuration);
builder.Services.AddPlatformModule(builder.Configuration);
builder.Services.AddAuditModule(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<TenantGuardMiddleware>();

app.MapIdentityModule();
app.MapGet("/demo", () => "Hello, World!").RequireAuthorization();

app.Run();
