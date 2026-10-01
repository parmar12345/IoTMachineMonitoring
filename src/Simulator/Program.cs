using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Simulator;
using Simulator.Mqtt;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<MqttOptions>(
    builder.Configuration.GetSection("Mqtt"));

builder.Services.Configure<SimulatorOptions>(
    builder.Configuration.GetSection("Simulator"));

builder.Services.AddHostedService<Worker>();

var host = builder.Build();

await host.RunAsync();