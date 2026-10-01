using IngestionWorker;
using IngestionWorker.Data;
using IngestionWorker.Mqtt;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<MqttOptions>(
    builder.Configuration.GetSection("Mqtt"));

var connectionString =
    builder.Configuration.GetConnectionString("TelemetryDb");

builder.Services.AddPooledDbContextFactory<TelemetryDbContext>(
    options =>
        options.UseNpgsql(connectionString));

builder.Services.AddSingleton<TelemetryChannel>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();

await host.RunAsync();