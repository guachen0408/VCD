using VacuumDryer.Core.Alarm;
using VacuumDryer.Core.Motion;
using VacuumDryer.Core.Process.Engine;
using VacuumDryer.Hardware;

namespace VacuumDryer.Core.Process;

/// <summary>
/// VacuumDryer 專案的流程環境實作
/// 提供動態旗標、專案特定服務、壓力模擬等
/// </summary>
public class VacuumProcessContext : IProcessContext
{
    private readonly Dictionary<Type, object> _services = new();
    private readonly AlarmManager _alarmManager = new();
    
    public Dictionary<string, bool> Flags { get; } = new();
    public string CurrentState { get; set; } = "Idle";
    public bool IsRunning { get; set; }
    public bool IsPaused { get; set; }
    public bool HasError { get; set; }
    
    // ===== VacuumDryer 專屬 =====
    
    /// <summary>目前壓力值 (Pa)</summary>
    public double CurrentPressure { get; set; } = 101325;
    
    /// <summary>目前細抽段數</summary>
    public int CurrentHighVacuumStage { get; set; }
    
    /// <summary>階段開始時間</summary>
    public DateTime StageStartTime { get; set; }
    
    /// <summary>製程參數</summary>
    public ProcessRecipe Recipe { get; set; } = new();
    
    /// <summary>警報管理器</summary>
    public AlarmManager AlarmManager => _alarmManager;
    
    // ===== 事件 =====
    public event Action<string, string>? OnStateChanged;
    public event Action<string>? OnLog;
    public event Action<int, string>? OnAlarm;
    
    // ===== 建構 =====
    
    public VacuumProcessContext()
    {
    }
    
    /// <summary>
    /// 註冊服務 (如 DualZController, IDigitalIO)
    /// </summary>
    public void RegisterService<T>(T service) where T : class
    {
        _services[typeof(T)] = service;
    }
    
    // ===== IProcessContext 實作 =====
    
    public T? GetService<T>() where T : class
    {
        return _services.TryGetValue(typeof(T), out var svc) ? svc as T : null;
    }
    
    public void Log(string message)
    {
        OnLog?.Invoke(message);
    }
    
    public void RaiseAlarm(int code, string message)
    {
        // 透過 AlarmManager 統一管理
        var severity = code < -110500 ? AlarmSeverity.Critical : AlarmSeverity.Error;
        _alarmManager.Raise(code, message, severity, CurrentState);
        
        OnAlarm?.Invoke(code, message);
        Log($"⚠️ 異常 {code}: {message}");
    }
    
    public void SetState(string state, string message)
    {
        CurrentState = state;
        Log(message);
        OnStateChanged?.Invoke(state, message);
    }
    
    public void ResetFlags()
    {
        Flags.Clear();
        CurrentPressure = 101325;
        CurrentHighVacuumStage = 0;
        IsRunning = false;
        IsPaused = false;
        HasError = false;
    }
}
