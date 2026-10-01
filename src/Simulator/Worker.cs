using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using MQTTnet;
using Shared.Models;
using Simulator.Mqtt;

namespace Simulator;

public sealed class Worker : BackgroundService
{
    private readonly MqttOptions _mqttOptions;
    private readonly SimulatorOptions _simulatorOptions;
    private readonly ILogger<Worker> _logger;

    public Worker(
        IOptions<MqttOptions> mqttOptions,
        IOptions<SimulatorOptions> simulatorOptions,
        ILogger<Worker> logger)
    {
        _mqttOptions = mqttOptions.Value;
        _simulatorOptions = simulatorOptions.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        var factory = new MqttClientFactory();

        using var mqttClient = factory.CreateMqttClient();

        var options = new MqttClientOptionsBuilder()
            .WithClientId(_mqttOptions.ClientId)
            .WithTcpServer(
                _mqttOptions.Host,
                _mqttOptions.Port)
            .Build();

        _logger.LogInformation(
            "Connecting to MQTT broker {Host}:{Port}",
            _mqttOptions.Host,
            _mqttOptions.Port);

        await mqttClient.ConnectAsync(
            options,
            stoppingToken);

        _logger.LogInformation(
            "Connected to MQTT broker.");

        var machines = CreateMachines();

        _logger.LogInformation(
            "Created {MachineCount} simulated machines.",
            machines.Count);

        while (!stoppingToken.IsCancellationRequested)
        {
            foreach (var machine in machines)
            {
                machine.Update();

                var telemetry = new TelemetryMessage
                {
                    V = 1,
                    Ts = DateTimeOffset.UtcNow,
                    Seq = machine.Sequence,
                    State = machine.State,
                    SpindleSpeedRpm = machine.SpindleSpeedRpm,
                    SpindleLoadPct = machine.SpindleLoadPct,
                    SpindleTempC = machine.SpindleTempC,
                    VibrationMmS = machine.VibrationMmS,
                    PowerKw = machine.PowerKw,
                    PartCount = machine.PartCount
                };

                var json = JsonSerializer.Serialize(telemetry);

                var topic =
                    $"factory/{machine.Site}/{machine.Line}/{machine.MachineId}/telemetry";

                var message = new MqttApplicationMessageBuilder()
                    .WithTopic(topic)
                    .WithPayload(Encoding.UTF8.GetBytes(json))
                    .Build();

                await mqttClient.PublishAsync(
                    message,
                    stoppingToken);

                _logger.LogInformation(
                    "Published {MachineId} seq {Sequence} state {State}",
                    machine.MachineId,
                    machine.Sequence,
                    machine.State);
            }

            await Task.Delay(
                _simulatorOptions.PublishIntervalMs,
                stoppingToken);
        }
    }

    private List<MachineSimulator> CreateMachines()
    {
        var machines = new List<MachineSimulator>();

        for (var i = 1;
             i <= _simulatorOptions.MachineCount;
             i++)
        {
            var lineNumber = ((i - 1) / 25) + 1;

            var machineNumber = ((i - 1) % 25) + 1;

            var line = $"line-{lineNumber}";

            var machineId = $"cnc-{machineNumber:000}";

            machines.Add(
     new MachineSimulator(
         "ahm-01",
         line,
         machineId,
         _simulatorOptions.FaultProbability));
        }

        return machines;
    }
}