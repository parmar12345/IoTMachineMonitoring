namespace Shared.Models;

public sealed class TelemetryMessage
{
    public int V { get; set; }
    public DateTimeOffset Ts { get; set; }
    public long Seq { get; set; }
    public string State { get; set; } = "Running";
    public double SpindleSpeedRpm { get; set; }
    public double SpindleLoadPct { get; set; }
    public double SpindleTempC { get; set; }
    public double VibrationMmS { get; set; }
    public double PowerKw { get; set; }
    public long PartCount { get; set; }
}