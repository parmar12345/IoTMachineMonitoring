namespace Shared.Models;

public sealed record TelemetryEnvelope(
    string Site,
    string Line,
    string MachineId,
    TelemetryMessage Telemetry);