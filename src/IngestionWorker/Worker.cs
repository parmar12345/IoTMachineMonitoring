using System.Text;
using System.Text.Json;
using IngestionWorker.Data;
using IngestionWorker.Mqtt;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MQTTnet;
using Shared.Models;

namespace IngestionWorker;

public sealed class Worker : BackgroundService
{
    private readonly MqttOptions _mqttOptions;
    private readonly TelemetryChannel _telemetryChannel;
    private readonly IDbContextFactory<TelemetryDbContext> _dbContextFactory;
    private readonly ILogger<Worker> _logger;

    public Worker(
        IOptions<MqttOptions> mqttOptions,
        TelemetryChannel telemetryChannel,
        IDbContextFactory<TelemetryDbContext> dbContextFactory,
        ILogger<Worker> logger)
    {
        _mqttOptions = mqttOptions.Value;
        _telemetryChannel = telemetryChannel;
        _dbContextFactory = dbContextFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        var factory = new MqttClientFactory();

        using var mqttClient = factory.CreateMqttClient();

        mqttClient.ApplicationMessageReceivedAsync +=
            async eventArgs =>
            {
                await HandleMessageAsync(
                    eventArgs,
                    stoppingToken);
            };

        var mqttOptions = new MqttClientOptionsBuilder()
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
            mqttOptions,
            stoppingToken);

        _logger.LogInformation(
            "Connected to MQTT broker.");

        var subscribeOptions =
            new MqttClientSubscribeOptionsBuilder()
                .WithTopicFilter(_mqttOptions.Topic)
                .Build();

        await mqttClient.SubscribeAsync(
            subscribeOptions,
            stoppingToken);

        _logger.LogInformation(
            "Subscribed to topic {Topic}",
            _mqttOptions.Topic);

        await ProcessTelemetryAsync(stoppingToken);
    }

    private async Task HandleMessageAsync(
        MqttApplicationMessageReceivedEventArgs eventArgs,
        CancellationToken stoppingToken)
    {
        try
        {
            var topic = eventArgs.ApplicationMessage.Topic;

            var payload = eventArgs.ApplicationMessage.Payload;

            var json = Encoding.UTF8.GetString(payload);

            var telemetry =
                JsonSerializer.Deserialize<TelemetryMessage>(json);

            if (telemetry is null)
            {
                _logger.LogWarning(
                    "Received empty telemetry payload from {Topic}",
                    topic);

                return;
            }

            var parts = topic.Split('/');

            if (parts.Length != 5)
            {
                _logger.LogWarning(
                    "Invalid MQTT topic: {Topic}",
                    topic);

                return;
            }

            var envelope = new TelemetryEnvelope(
                Site: parts[1],
                Line: parts[2],
                MachineId: parts[3],
                Telemetry: telemetry);

            await _telemetryChannel.Writer.WriteAsync(
                envelope,
                stoppingToken);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(
                ex,
                "Invalid telemetry JSON received.");
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
            // Normal shutdown.
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing MQTT message.");
        }
    }

    private async Task ProcessTelemetryAsync(
        CancellationToken stoppingToken)
    {
        var batch = new List<TelemetryRecord>(500);

        using var timer = new PeriodicTimer(
            TimeSpan.FromSeconds(1));

        while (!stoppingToken.IsCancellationRequested)
        {
            batch.Clear();

            while (batch.Count < 500)
            {
                var readTask =
                    _telemetryChannel.Reader.WaitToReadAsync(
                        stoppingToken)
                    .AsTask();

                var timerTask =
                    timer.WaitForNextTickAsync(
                        stoppingToken)
                    .AsTask();

                var completedTask =
                    await Task.WhenAny(
                        readTask,
                        timerTask);

                if (completedTask == readTask)
                {
                    if (!await readTask)
                    {
                        return;
                    }

                    while (
                        batch.Count < 500 &&
                        _telemetryChannel.Reader.TryRead(
                            out var envelope))
                    {
                        batch.Add(
                            ConvertToRecord(envelope));
                    }
                }
                else
                {
                    break;
                }
            }

            if (batch.Count > 0)
            {
                await SaveBatchAsync(
                    batch,
                    stoppingToken);
            }
        }
    }

    private static TelemetryRecord ConvertToRecord(
        TelemetryEnvelope envelope)
    {
        var telemetry = envelope.Telemetry;

        return new TelemetryRecord
        {
            Site = envelope.Site,
            Line = envelope.Line,
            MachineId = envelope.MachineId,
            Timestamp = telemetry.Ts,
            Sequence = telemetry.Seq,
            State = telemetry.State,
            SpindleSpeedRpm = telemetry.SpindleSpeedRpm,
            SpindleLoadPct = telemetry.SpindleLoadPct,
            SpindleTempC = telemetry.SpindleTempC,
            VibrationMmS = telemetry.VibrationMmS,
            PowerKw = telemetry.PowerKw,
            PartCount = telemetry.PartCount
        };
    }

    private async Task SaveBatchAsync(
        List<TelemetryRecord> batch,
        CancellationToken stoppingToken)
    {
        await using var db =
            await _dbContextFactory.CreateDbContextAsync(
                stoppingToken);

        await db.Telemetry.AddRangeAsync(
            batch,
            stoppingToken);

        await db.SaveChangesAsync(
            stoppingToken);

        _logger.LogInformation(
            "Saved {Count} telemetry records to PostgreSQL.",
            batch.Count);
    }
}