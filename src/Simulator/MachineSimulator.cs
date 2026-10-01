namespace Simulator;

public sealed class MachineSimulator
{
    private readonly Random _random = new();

    private readonly double _faultProbability;

    public string Site { get; }

    public string Line { get; }

    public string MachineId { get; }

    public string State { get; private set; } = "Running";

    public double SpindleSpeedRpm { get; private set; } = 4200;

    public double SpindleLoadPct { get; private set; } = 70;

    public double SpindleTempC { get; private set; } = 58;

    public double VibrationMmS { get; private set; } = 2;

    public double PowerKw { get; private set; } = 18;

    public long PartCount { get; private set; }

    public long Sequence { get; private set; }

    public MachineSimulator(
        string site,
        string line,
        string machineId,
        double faultProbability)
    {
        Site = site;
        Line = line;
        MachineId = machineId;
        _faultProbability = faultProbability;
    }

    public void Update()
    {
        Sequence++;

        UpdateState();
        UpdateSignals();
        UpdatePartCount();
    }

    private void UpdateState()
    {
        var roll = _random.NextDouble();

        if (State == "Running" &&
            roll < _faultProbability)
        {
            State = "Down";

            return;
        }

        if (State == "Down" &&
            roll < 0.05)
        {
            State = "Running";
        }
    }

    private void UpdateSignals()
    {
        if (State == "Down")
        {
            SpindleSpeedRpm = 0;
            SpindleLoadPct = 0;
            PowerKw = 1;

            SpindleTempC = Math.Max(
                30,
                SpindleTempC - 0.5);

            VibrationMmS = Math.Max(
                0.5,
                VibrationMmS - 0.1);

            return;
        }

        SpindleSpeedRpm = Clamp(
            SpindleSpeedRpm + RandomDelta(150),
            3500,
            4500);

        SpindleLoadPct = Clamp(
            SpindleLoadPct + RandomDelta(5),
            40,
            90);

        SpindleTempC = Clamp(
            SpindleTempC + RandomDelta(0.8),
            50,
            75);

        VibrationMmS = Clamp(
            VibrationMmS + RandomDelta(0.3),
            1,
            5);

        PowerKw = Clamp(
            PowerKw + RandomDelta(1),
            10,
            25);
    }

    private void UpdatePartCount()
    {
        if (State == "Running")
        {
            PartCount++;
        }
    }

    private double RandomDelta(double maximum)
    {
        return (_random.NextDouble() * 2 - 1) * maximum;
    }

    private static double Clamp(
        double value,
        double minimum,
        double maximum)
    {
        return Math.Clamp(
            value,
            minimum,
            maximum);
    }
}