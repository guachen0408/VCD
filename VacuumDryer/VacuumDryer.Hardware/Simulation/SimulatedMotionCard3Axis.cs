namespace VacuumDryer.Hardware.Simulation;

/// <summary>
/// 模擬運動控制卡 (3軸版本) - Z1, Z2 龍門軸 + 蝶閥軸
/// </summary>
public class SimulatedMotionCard3Axis : IMotionCard
{
    private readonly List<SimulatedAxis> _axes = new();
    private readonly SimulatedDigitalIO _digitalIO = new();
    private bool _isConnected;

    public string Name => "Simulated 3-Axis Motion Card (Delta EtherCAT)";
    public bool IsConnected => _isConnected;
    public int AxisCount => _axes.Count;

    public SimulatedMotionCard3Axis()
    {
        _axes.Add(new SimulatedAxis(0, "Z1"));
        _axes.Add(new SimulatedAxis(1, "Z2"));
        _axes.Add(new SimulatedAxis(2, "Valve"));
    }

    public Task<bool> InitializeAsync()
    {
        _isConnected = true;
        return Task.FromResult(true);
    }

    public IAxis GetAxis(int axisId)
    {
        if (axisId < 0 || axisId >= _axes.Count)
            throw new ArgumentOutOfRangeException(nameof(axisId));
        return _axes[axisId];
    }

    public IEnumerable<IAxis> GetAllAxes() => _axes;
    public IDigitalIO? GetDigitalIO() => _digitalIO;

    public async Task EmergencyStopAllAsync()
    {
        foreach (var axis in _axes)
            await axis.EmergencyStopAsync();
    }

    public Task CloseAsync()
    {
        _isConnected = false;
        return Task.CompletedTask;
    }

    public void Dispose() => CloseAsync().Wait();
}

/// <summary>
/// 模擬數位 IO
/// </summary>
public class SimulatedDigitalIO : IDigitalIO
{
    private readonly bool[] _inputs = new bool[16];
    private readonly bool[] _outputs = new bool[16];

    public bool ReadInput(int channel) => _inputs[channel];
    public bool[] ReadAllInputs() => _inputs.ToArray();
    public void WriteOutput(int channel, bool value) => _outputs[channel] = value;
    public bool ReadOutput(int channel) => _outputs[channel];
    public bool[] ReadAllOutputs() => _outputs.ToArray();
    public void SimulateInput(int channel, bool value) => _inputs[channel] = value;
}
