namespace Swivel.Core;

/// <summary>Random-walk traffic series driving the live charts.</summary>
public sealed class Sim
{
    public const int Window = 90;

    readonly Random _random;
    readonly List<double> _cpu = [];
    readonly List<double> _down = [];
    readonly List<double> _up = [];

    double _cpuTarget;
    double _cpuValue;
    double _downTarget;
    double _downValue;
    double _upTarget;
    double _upValue;
    readonly long _startedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    public IReadOnlyList<double> Cpu => _cpu;
    public double Latency { get; private set; } = 42.0;
    public long ElapsedSeconds => (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - _startedAt) / 1000;

    public Sim()
        : this(new Random(), seed: true)
    {
    }

    /// <summary>Test seam: a fixed seed makes the series reproducible.</summary>
    public Sim(int seed)
        : this(new Random(seed), seed: true)
    {
    }

    Sim(Random random, bool seed)
    {
        _random = random;
        _cpuTarget = _cpuValue = _random.NextDouble() * 30 + 10;
        _downTarget = _downValue = _random.NextDouble() * 15 + 3;
        _upTarget = _upValue = _random.NextDouble() * 5.5 + .5;
        for (var i = 0; i < Window; i++)
        {
            _cpu.Add(_cpuValue);
            _down.Add(_downValue);
            _up.Add(_upValue);
        }
    }

    /// <summary>Nudge every series toward its target and append a new sample.</summary>
    public void Step()
    {
        if (_random.NextDouble() < .05)
            _cpuTarget = _random.NextDouble() * 90 + 5;
        _cpuValue = Math.Clamp(_cpuValue + (_cpuTarget - _cpuValue) * .1
            + (_random.NextDouble() * 3.6 - 1.8), 2, 99);
        Push(_cpu, _cpuValue);

        if (_random.NextDouble() < .1)
        {
            _downTarget = _random.NextDouble() * 59.5 + .5;
            _upTarget = _random.NextDouble() * 17.8 + .2;
        }
        _downValue = Math.Clamp(_downValue + (_downTarget - _downValue) * .15
            + (_random.NextDouble() * 4 - 2), .1, 80);
        _upValue = Math.Clamp(_upValue + (_upTarget - _upValue) * .15
            + (_random.NextDouble() * 1.6 - .8), .05, 30);
        Push(_down, _downValue);
        Push(_up, _upValue);

        Latency = Math.Clamp(Latency + (_random.NextDouble() * 6 - 3), 12, 180);
    }

    void Push(List<double> series, double value)
    {
        series.Add(value);
        if (series.Count > Window)
            series.RemoveRange(0, series.Count - Window);
    }
}