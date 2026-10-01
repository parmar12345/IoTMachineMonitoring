namespace IngestionWorker.Data;

public sealed class TelemetryRecord
{
    public long Id { get; set; }

    public string Site { get; set; } = string.Empty;

    public string Line { get; set; } = string.Empty;

    public string MachineId { get; set; } = string.Empty;

    public DateTimeOffset Timestamp { get; set; }

    public long Sequence { get; set; }

    public string State { get; set; } = string.Empty;

    public double SpindleSpeedRpm { get; set; }

    public double SpindleLoadPct { get; set; }

    public double SpindleTempC { get; set; }

    public double VibrationMmS { get; set; }

    public double PowerKw { get; set; }

    public long PartCount { get; set; }
}