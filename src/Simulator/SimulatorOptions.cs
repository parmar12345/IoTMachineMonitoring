namespace Simulator;

public sealed class SimulatorOptions
{
    public int MachineCount { get; set; } = 1;

    public int PublishIntervalMs { get; set; } = 1000;

    public double FaultProbability { get; set; } = 0.01;
}