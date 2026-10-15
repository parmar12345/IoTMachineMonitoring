using IngestionWorker.Data;
using IngestionWorker.Mqtt;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MQTTnet;
using Shared.Models;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

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
        const int batchSize = 500;

        var maxBatchWait = TimeSpan.FromSeconds(1);

        var batch = new List<TelemetryRecord>(batchSize);

        try
        {
            while (await _telemetryChannel.Reader.WaitToReadAsync(
                stoppingToken))
            {
                batch.Clear();

                var batchTimer = Stopwatch.StartNew();
                var channelCompleted = false;

                while (batch.Count < batchSize)
                {
                    // Drain all currently available records.
                    while (
                        batch.Count < batchSize &&
                        _telemetryChannel.Reader.TryRead(
                            out var envelope))
                    {
                        batch.Add(ConvertToRecord(envelope));
                    }

                    // Save immediately when the batch reaches 500.
                    if (batch.Count >= batchSize)
                    {
                        break;
                    }

                    // Flush a partial batch after the maximum wait.
                    var remaining =
                        maxBatchWait - batchTimer.Elapsed;

                    if (remaining <= TimeSpan.Zero)
                    {
                        break;
                    }

                    // Wait for more data instead of polling every second.
                    using var waitCts =
                        CancellationTokenSource
                            .CreateLinkedTokenSource(stoppingToken);

                    waitCts.CancelAfter(remaining);

                    try
                    {
                        var hasMoreData =
                            await _telemetryChannel.Reader
                                .WaitToReadAsync(waitCts.Token);

                        if (!hasMoreData)
                        {
                            channelCompleted = true;
                            break;
                        }
                    }
                    catch (OperationCanceledException)
                        when (!stoppingToken.IsCancellationRequested)
                    {
                        // The batch collection timeout expired.
                        break;
                    }
                }

                if (batch.Count > 0)
                {
                    await SaveBatchAsync(
                        batch,
                        stoppingToken);
                }

                if (channelCompleted)
                {
                    break;
                }
            }
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation(
                "Telemetry batch processing is stopping.");
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
        var batchCount = batch.Count;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await using var db =
                await _dbContextFactory.CreateDbContextAsync(
                    stoppingToken);

            // Keep historical inserts and latest-state updates
            // in one transaction.
            await using var transaction =
                await db.Database.BeginTransactionAsync(
                    stoppingToken);

            // 1. Save every telemetry record to historical storage.
            await db.Telemetry.AddRangeAsync(
                batch,
                stoppingToken);

            await db.SaveChangesAsync(stoppingToken);

            // 2. Keep only the newest record per machine in this batch.
            // This avoids duplicate conflict keys in a single INSERT.
            var latestPerMachine = batch
                .GroupBy(x => new
                {
                    x.Site,
                    x.Line,
                    x.MachineId
                })
                .Select(group => group
                    .OrderByDescending(x => x.Timestamp)
                    .ThenByDescending(x => x.Sequence)
                    .First())
                .ToList();

            // 3. Build one parameterized PostgreSQL upsert.
            var sql = new StringBuilder();
            var parameters = new List<object>();

            sql.Append("""
            INSERT INTO public.machine_latest_telemetry AS current
            (
                "Id", "Site", "Line", "MachineId",
                "Timestamp", "Sequence", "State",
                "SpindleSpeedRpm", "SpindleLoadPct",
                "SpindleTempC", "VibrationMmS",
                "PowerKw", "PartCount"
            )
            VALUES
            """);

            for (var i = 0; i < latestPerMachine.Count; i++)
            {
                var record = latestPerMachine[i];

                if (i > 0)
                {
                    sql.Append(',');
                }

                sql.Append('(');

                var values = new object[]
                {
                record.Id,
                record.Site,
                record.Line,
                record.MachineId,
                record.Timestamp,
                record.Sequence,
                record.State,
                record.SpindleSpeedRpm,
                record.SpindleLoadPct,
                record.SpindleTempC,
                record.VibrationMmS,
                record.PowerKw,
                record.PartCount
                };

                for (var j = 0; j < values.Length; j++)
                {
                    if (j > 0)
                    {
                        sql.Append(", ");
                    }

                    var parameterName = $"p{parameters.Count}";

                    sql.Append('@').Append(parameterName);

                    parameters.Add(
                        new Npgsql.NpgsqlParameter(
                            parameterName,
                            values[j]));
                }

                sql.Append(')');
            }

            sql.Append("""
            
            ON CONFLICT ("Site", "Line", "MachineId")
            DO UPDATE SET
                "Id" = EXCLUDED."Id",
                "Timestamp" = EXCLUDED."Timestamp",
                "Sequence" = EXCLUDED."Sequence",
                "State" = EXCLUDED."State",
                "SpindleSpeedRpm" = EXCLUDED."SpindleSpeedRpm",
                "SpindleLoadPct" = EXCLUDED."SpindleLoadPct",
                "SpindleTempC" = EXCLUDED."SpindleTempC",
                "VibrationMmS" = EXCLUDED."VibrationMmS",
                "PowerKw" = EXCLUDED."PowerKw",
                "PartCount" = EXCLUDED."PartCount"
            WHERE
                (EXCLUDED."Timestamp", EXCLUDED."Sequence")
                >
                (current."Timestamp", current."Sequence");
            """);

            await db.Database.ExecuteSqlRawAsync(
                sql.ToString(),
                parameters,
                stoppingToken);

            await transaction.CommitAsync(stoppingToken);
        }
        finally
        {
            stopwatch.Stop();
        }

        _logger.LogInformation(
            "Saved {Count} telemetry records to PostgreSQL in " +
            "{DurationMs:F2} ms. Queue depth: {QueueDepth}.",
            batchCount,
            stopwatch.Elapsed.TotalMilliseconds,
            _telemetryChannel.Count);
    }


}