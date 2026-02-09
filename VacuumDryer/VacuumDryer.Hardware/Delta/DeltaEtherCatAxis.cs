namespace VacuumDryer.Hardware.Delta;

/// <summary>
/// 台達 EtherCAT 軸實作 - PCI-L221B1D0
/// </summary>
public class DeltaEtherCatAxis : IAxis
{
    private readonly int _cardId;
    private readonly int _axisId;
    private readonly string _name;
    private readonly double _pulsesPerMm;
    
    private double _targetPosition;
    private double _acceleration = 1000;
    private double _deceleration = 1000;

    public int AxisId => _axisId;
    public string Name => _name;
    
    public double Position
    {
        get
        {
            EtherCatDll.Ecat_GetActualPos(_cardId, _axisId, out double pos);
            return pos / _pulsesPerMm;
        }
    }
    
    public double TargetPosition => _targetPosition;
    
    public double Velocity
    {
        get
        {
            EtherCatDll.Ecat_GetActualVel(_cardId, _axisId, out double vel);
            return vel / _pulsesPerMm;
        }
    }
    
    public double Torque
    {
        get
        {
            EtherCatDll.Ecat_GetActualTorque(_cardId, _axisId, out double torque);
            return torque;
        }
    }
    
    public AxisState State
    {
        get
        {
            EtherCatDll.Ecat_GetAxisStatus(_cardId, _axisId, out uint status);
            EtherCatDll.Ecat_IsServoOn(_cardId, _axisId, out int isOn);
            EtherCatDll.Ecat_IsMoving(_cardId, _axisId, out int moving);
            
            if ((status & 0x08) != 0) return AxisState.Alarm;
            if (moving != 0) return AxisState.Moving;
            if (isOn != 0) return AxisState.Enabled;
            return AxisState.Disabled;
        }
    }
    
    public bool IsMoving
    {
        get
        {
            EtherCatDll.Ecat_IsMoving(_cardId, _axisId, out int moving);
            return moving != 0;
        }
    }
    
    public bool IsHomed { get; private set; }
    
    public bool PositiveLimitTriggered
    {
        get
        {
            EtherCatDll.Ecat_GetAxisStatus(_cardId, _axisId, out uint status);
            return (status & 0x01) != 0;
        }
    }
    
    public bool NegativeLimitTriggered
    {
        get
        {
            EtherCatDll.Ecat_GetAxisStatus(_cardId, _axisId, out uint status);
            return (status & 0x02) != 0;
        }
    }

    public DeltaEtherCatAxis(int cardId, int axisId, string name, double pulsesPerMm = 10000)
    {
        _cardId = cardId;
        _axisId = axisId;
        _name = name;
        _pulsesPerMm = pulsesPerMm;
    }

    public Task<bool> EnableAsync()
    {
        var result = EtherCatDll.Ecat_ServoOn(_cardId, _axisId);
        return Task.FromResult(result == EtherCatError.SUCCESS);
    }

    public Task<bool> DisableAsync()
    {
        var result = EtherCatDll.Ecat_ServoOff(_cardId, _axisId);
        return Task.FromResult(result == EtherCatError.SUCCESS);
    }

    public async Task<bool> HomeAsync(CancellationToken cancellationToken = default)
    {
        var result = EtherCatDll.Ecat_Home(_cardId, _axisId, 35);
        if (result != EtherCatError.SUCCESS) return false;
        
        while (!cancellationToken.IsCancellationRequested)
        {
            result = EtherCatDll.Ecat_WaitHomeDone(_cardId, _axisId, 100);
            if (result == EtherCatError.SUCCESS)
            {
                IsHomed = true;
                return true;
            }
            if (result != EtherCatError.ERR_TIMEOUT) return false;
            await Task.Delay(100, cancellationToken);
        }
        return false;
    }

    public async Task<bool> MoveAbsoluteAsync(double position, double velocity, CancellationToken cancellationToken = default)
    {
        _targetPosition = position;
        
        var posPulse = position * _pulsesPerMm;
        var velPulse = velocity * _pulsesPerMm;
        var accPulse = _acceleration * _pulsesPerMm;
        var decPulse = _deceleration * _pulsesPerMm;
        
        var result = EtherCatDll.Ecat_AbsMove(_cardId, _axisId, posPulse, velPulse, accPulse, decPulse);
        if (result != EtherCatError.SUCCESS) return false;
        
        while (!cancellationToken.IsCancellationRequested && IsMoving)
            await Task.Delay(50, cancellationToken);
        
        return !cancellationToken.IsCancellationRequested;
    }

    public async Task<bool> MoveRelativeAsync(double distance, double velocity, CancellationToken cancellationToken = default)
    {
        _targetPosition = Position + distance;
        
        var distPulse = distance * _pulsesPerMm;
        var velPulse = velocity * _pulsesPerMm;
        var accPulse = _acceleration * _pulsesPerMm;
        var decPulse = _deceleration * _pulsesPerMm;
        
        var result = EtherCatDll.Ecat_RelMove(_cardId, _axisId, distPulse, velPulse, accPulse, decPulse);
        if (result != EtherCatError.SUCCESS) return false;
        
        while (!cancellationToken.IsCancellationRequested && IsMoving)
            await Task.Delay(50, cancellationToken);
        
        return !cancellationToken.IsCancellationRequested;
    }

    public Task<bool> JogAsync(double velocity)
    {
        var velPulse = velocity * _pulsesPerMm;
        var accPulse = _acceleration * _pulsesPerMm;
        var decPulse = _deceleration * _pulsesPerMm;
        
        var result = EtherCatDll.Ecat_JogMove(_cardId, _axisId, velPulse, accPulse, decPulse);
        return Task.FromResult(result == EtherCatError.SUCCESS);
    }

    public Task<bool> StopAsync()
    {
        var decPulse = _deceleration * _pulsesPerMm;
        var result = EtherCatDll.Ecat_Stop(_cardId, _axisId, decPulse);
        return Task.FromResult(result == EtherCatError.SUCCESS);
    }

    public Task<bool> EmergencyStopAsync()
    {
        var result = EtherCatDll.Ecat_EmgStop(_cardId, _axisId);
        return Task.FromResult(result == EtherCatError.SUCCESS);
    }

    public Task<bool> ClearAlarmAsync()
    {
        var result = EtherCatDll.Ecat_ClearAlarm(_cardId, _axisId);
        return Task.FromResult(result == EtherCatError.SUCCESS);
    }
}
