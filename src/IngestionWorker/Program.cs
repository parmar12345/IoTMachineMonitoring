using IngestionWorker.Mqtt;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<MqttOptions>(
    builder.Configuration.GetSection("Mqtt"));

builder.Services.AddHostedService<Worker>();

var host = builder.Build();

host.Run();