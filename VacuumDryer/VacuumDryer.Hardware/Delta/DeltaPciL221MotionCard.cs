namespace VacuumDryer.Hardware.Delta;

/// <summary>
/// 台達 PCI-L221 EtherCAT 運動控制卡實作
/// </summary>
public class DeltaPciL221MotionCard : IMotionCard
{
    private readonly int _cardId;
    private readonly List<DeltaEtherCatAxis> _axes = new();
    private readonly DeltaEtherCatDigitalIO _digitalIO;
    private bool _isConnected;

    public string Name => $"Delta PCI-L221B1D0 (Card {_cardId})";
    public bool IsConnected => _isConnected;
    public int AxisCount => _axes.Count;

    public DeltaPciL221MotionCard(int cardId = 0, IEnumerable<AxisConfig>? axisConfigs = null)
    {
        _cardId = cardId;
        _digitalIO = new DeltaEtherCatDigitalIO(_cardId);
        
        var configs = axisConfigs?.ToList() ?? new List<AxisConfig>
        {
            new(0, "Z1", 10000),
            new(1, "Z2", 10000),
            new(2, "Valve", 1000)
        };
        
        foreach (var config in configs)
            _axes.Add(new DeltaEtherCatAxis(_cardId, config.AxisId, config.Name, config.PulsesPerUnit));
    }

    public Task<bool> InitializeAsync()
    {
        var result = EtherCatDll.Ecat_Init(_cardId);
        _isConnected = (result == EtherCatError.SUCCESS);
        
        if (!_isConnected)
            throw new InvalidOperationException($"無法初始化台達 EtherCAT 控制卡！錯誤碼: {result}");
        
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
        EtherCatDll.Ecat_Close(_cardId);
        _isConnected = false;
        return Task.CompletedTask;
    }

    public void Dispose() => CloseAsync().Wait();
}

public record AxisConfig(int AxisId, string Name, double PulsesPerUnit);

public class DeltaEtherCatDigitalIO : IDigitalIO
{
    private readonly int _cardId;

    public DeltaEtherCatDigitalIO(int cardId) => _cardId = cardId;

    public bool ReadInput(int channel)
    {
        EtherCatDll.Ecat_ReadDI(_cardId, channel, out int value);
        return value != 0;
    }

    public bool[] ReadAllInputs()
    {
        var inputs = new bool[16];
        for (int i = 0; i < 16; i++) inputs[i] = ReadInput(i);
        return inputs;
    }

    public void WriteOutput(int channel, bool value) =>
        EtherCatDll.Ecat_WriteDO(_cardId, channel, value ? 1 : 0);

    public bool ReadOutput(int channel)
    {
        EtherCatDll.Ecat_ReadDO(_cardId, channel, out int value);
        return value != 0;
    }

    public bool[] ReadAllOutputs()
    {
        var outputs = new bool[16];
        for (int i = 0; i < 16; i++) outputs[i] = ReadOutput(i);
        return outputs;
    }
}
