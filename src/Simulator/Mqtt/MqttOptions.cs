namespace Simulator.Mqtt;

public sealed class MqttOptions
{
    public string Host { get; set; } = "localhost";

    public int Port { get; set; } = 1883;

    public string ClientId { get; set; } = "iot-simulator";

    public string Topic { get; set; } =
        "factory/{site}/{line}/{machine}/telemetry";
}