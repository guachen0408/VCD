namespace VacuumDryer.Hardware;

/// <summary>
/// 軸狀態列舉
/// </summary>
public enum AxisState
{
    Unknown,
    Disabled,
    Enabled,
    Moving,
    Homing,
    Error,
    Alarm
}

/// <summary>
/// 軸介面 - 定義單軸控制的基本操作
/// </summary>
public interface IAxis
{
    /// <summary>軸編號</summary>
    int AxisId { get; }
    
    /// <summary>軸名稱</summary>
    string Name { get; }
    
    /// <summary>當前位置 (脈衝數或 mm)</summary>
    double Position { get; }
    
    /// <summary>目標位置</summary>
    double TargetPosition { get; }
    
    /// <summary>當前速度</summary>
    double Velocity { get; }
    
    /// <summary>當前扭矩 (%)</summary>
    double Torque { get; }
    
    /// <summary>軸狀態</summary>
    AxisState State { get; }
    
    /// <summary>是否在移動中</summary>
    bool IsMoving { get; }
    
    /// <summary>是否已回原點</summary>
    bool IsHomed { get; }
    
    /// <summary>正極限觸發</summary>
    bool PositiveLimitTriggered { get; }
    
    /// <summary>負極限觸發</summary>
    bool NegativeLimitTriggered { get; }

    // === 控制方法 ===
    
    /// <summary>啟用伺服</summary>
    Task<bool> EnableAsync();
    
    /// <summary>停用伺服</summary>
    Task<bool> DisableAsync();
    
    /// <summary>執行回原點</summary>
    Task<bool> HomeAsync(CancellationToken cancellationToken = default);
    
    /// <summary>絕對位置移動</summary>
    Task<bool> MoveAbsoluteAsync(double position, double velocity, CancellationToken cancellationToken = default);
    
    /// <summary>相對位置移動</summary>
    Task<bool> MoveRelativeAsync(double distance, double velocity, CancellationToken cancellationToken = default);
    
    /// <summary>Jog 移動 (持續移動)</summary>
    Task<bool> JogAsync(double velocity);
    
    /// <summary>停止移動</summary>
    Task<bool> StopAsync();
    
    /// <summary>緊急停止</summary>
    Task<bool> EmergencyStopAsync();
    
    /// <summary>清除告警</summary>
    Task<bool> ClearAlarmAsync();
}
