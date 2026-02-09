namespace VacuumDryer.Hardware.Simulation;

/// <summary>
/// 模擬軸 - 用於開發與測試
/// </summary>
public class SimulatedAxis : IAxis
{
    private double _position;
    private double _targetPosition;
    private double _velocity;
    private bool _isMoving;
    private bool _isEnabled;
    private bool _isHomed;
    private CancellationTokenSource? _moveCts;

    public int AxisId { get; }
    public string Name { get; }
    public double Position => _position;
    public double TargetPosition => _targetPosition;
    public double Velocity => _velocity;
    public double Torque => _isMoving ? 30.0 : 0.0;
    public AxisState State => _isMoving ? AxisState.Moving : (_isEnabled ? AxisState.Enabled : AxisState.Disabled);
    public bool IsMoving => _isMoving;
    public bool IsHomed => _isHomed;
    public bool PositiveLimitTriggered => _position >= 500;
    public bool NegativeLimitTriggered => _position <= -500;

    public SimulatedAxis(int axisId, string name)
    {
        AxisId = axisId;
        Name = name;
    }

    public Task<bool> EnableAsync()
    {
        _isEnabled = true;
        return Task.FromResult(true);
    }

    public Task<bool> DisableAsync()
    {
        _isEnabled = false;
        return Task.FromResult(true);
    }

    public async Task<bool> HomeAsync(CancellationToken cancellationToken = default)
    {
        await MoveAbsoluteAsync(0, 50, cancellationToken);
        _isHomed = true;
        return true;
    }

    public async Task<bool> MoveAbsoluteAsync(double position, double velocity, CancellationToken cancellationToken = default)
    {
        _targetPosition = position;
        _velocity = velocity;
        _isMoving = true;
        
        var distance = Math.Abs(position - _position);
        var duration = distance / velocity * 1000;
        var steps = (int)(duration / 50);
        var stepSize = (position - _position) / Math.Max(steps, 1);
        
        for (int i = 0; i < steps && !cancellationToken.IsCancellationRequested; i++)
        {
            await Task.Delay(50, cancellationToken);
            _position += stepSize;
        }
        
        _position = position;
        _isMoving = false;
        _velocity = 0;
        return true;
    }

    public async Task<bool> MoveRelativeAsync(double distance, double velocity, CancellationToken cancellationToken = default)
    {
        return await MoveAbsoluteAsync(_position + distance, velocity, cancellationToken);
    }

    public Task<bool> JogAsync(double velocity)
    {
        _moveCts?.Cancel();
        _moveCts = new CancellationTokenSource();
        _velocity = velocity;
        _isMoving = true;
        
        _ = Task.Run(async () =>
        {
            while (!_moveCts.Token.IsCancellationRequested)
            {
                _position += velocity * 0.05;
                await Task.Delay(50);
            }
            _isMoving = false;
            _velocity = 0;
        });
        
        return Task.FromResult(true);
    }

    public Task<bool> StopAsync()
    {
        _moveCts?.Cancel();
        _isMoving = false;
        _velocity = 0;
        return Task.FromResult(true);
    }

    public Task<bool> EmergencyStopAsync()
    {
        _moveCts?.Cancel();
        _isMoving = false;
        _velocity = 0;
        return Task.FromResult(true);
    }

    public Task<bool> ClearAlarmAsync()
    {
        return Task.FromResult(true);
    }
}
