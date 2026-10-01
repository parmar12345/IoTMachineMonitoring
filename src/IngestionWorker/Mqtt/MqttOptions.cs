namespace IngestionWorker.Mqtt;

public sealed class MqttOptions
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 1883;
    public string ClientId { get; set; } = "iot-ingestion-worker";
    public string Topic { get; set; } = "factory/+/+/+/telemetry";
}