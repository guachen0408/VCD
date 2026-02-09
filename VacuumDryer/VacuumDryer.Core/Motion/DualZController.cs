using VacuumDryer.Hardware;

namespace VacuumDryer.Core.Motion;

/// <summary>
/// 龍門 Z 軸控制器 - 管理雙驅 Z 軸同步運動與蝶閥控制
/// </summary>
public class DualZController
{
    private readonly IAxis _z1Axis;
    private readonly IAxis _z2Axis;
    private readonly IAxis _valveAxis;
    
    public double SyncTolerance { get; set; } = 0.1;
    public double PositionZ => (_z1Axis.Position + _z2Axis.Position) / 2;
    public double ValvePosition => _valveAxis.Position;
    public double PositionZ1 => _z1Axis.Position;
    public double PositionZ2 => _z2Axis.Position;
    public double SyncError => Math.Abs(_z1Axis.Position - _z2Axis.Position);
    public bool IsSyncOk => SyncError <= SyncTolerance;
    public bool IsMoving => _z1Axis.IsMoving || _z2Axis.IsMoving || _valveAxis.IsMoving;
    
    public IAxis Z1Axis => _z1Axis;
    public IAxis Z2Axis => _z2Axis;
    public IAxis ValveAxis => _valveAxis;

    public DualZController(IMotionCard motionCard)
    {
        _z1Axis = motionCard.GetAxis(0);
        _z2Axis = motionCard.GetAxis(1);
        _valveAxis = motionCard.GetAxis(2);
    }

    public async Task<bool> EnableAllAsync()
    {
        var results = await Task.WhenAll(
            _z1Axis.EnableAsync(),
            _z2Axis.EnableAsync(),
            _valveAxis.EnableAsync()
        );
        return results.All(r => r);
    }

    public async Task<bool> DisableAllAsync()
    {
        var results = await Task.WhenAll(
            _z1Axis.DisableAsync(),
            _z2Axis.DisableAsync(),
            _valveAxis.DisableAsync()
        );
        return results.All(r => r);
    }

    public async Task<bool> HomeAllAsync(CancellationToken cancellationToken = default)
    {
        var zResults = await Task.WhenAll(
            _z1Axis.HomeAsync(cancellationToken),
            _z2Axis.HomeAsync(cancellationToken)
        );
        var valveResult = await _valveAxis.HomeAsync(cancellationToken);
        return zResults.All(r => r) && valveResult;
    }

    public async Task<bool> MoveZAsync(double position, double velocity, CancellationToken cancellationToken = default)
    {
        if (!IsSyncOk)
            throw new InvalidOperationException($"同步異常！誤差: {SyncError:F3} mm");

        var results = await Task.WhenAll(
            _z1Axis.MoveAbsoluteAsync(position, velocity, cancellationToken),
            _z2Axis.MoveAbsoluteAsync(position, velocity, cancellationToken)
        );
        return results.All(r => r);
    }

    /// <summary>
    /// Z軸同步移動 (流程控制使用)
    /// </summary>
    public Task<bool> MoveSyncAsync(double position, double velocity, CancellationToken cancellationToken = default)
        => MoveZAsync(position, velocity, cancellationToken);


    public async Task<bool> SetValveAsync(double openPercent, double velocity, CancellationToken cancellationToken = default)
    {
        openPercent = Math.Clamp(openPercent, 0, 100);
        return await _valveAxis.MoveAbsoluteAsync(openPercent, velocity, cancellationToken);
    }

    public async Task EmergencyStopAsync()
    {
        await Task.WhenAll(
            _z1Axis.EmergencyStopAsync(),
            _z2Axis.EmergencyStopAsync(),
            _valveAxis.EmergencyStopAsync()
        );
    }

    public async Task StopAllAsync()
    {
        await Task.WhenAll(
            _z1Axis.StopAsync(),
            _z2Axis.StopAsync(),
            _valveAxis.StopAsync()
        );
    }
}
