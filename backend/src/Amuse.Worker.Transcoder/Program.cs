using Amuse.Modules.Catalog;
using Amuse.Modules.Media;
using Amuse.Worker.Transcoder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddCatalogTranscoderServices(builder.Configuration);
builder.Services.AddMediaModule(builder.Configuration);
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection("RabbitMq"));

builder.Services.AddHostedService<TranscodingWorker>();

await builder.Build().RunAsync();

