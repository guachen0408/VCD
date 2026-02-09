using VacuumDryer.Core.Motion;
using VacuumDryer.Hardware;

namespace VacuumDryer.Core.Process;

/// <summary>
/// VCD 真空乾燥流程控制器
/// 參考 T23Y043 VCD 軟體操作手冊
/// </summary>
public class VacuumProcessController
{
    private readonly DualZController _motionController;
    private readonly IDigitalIO? _digitalIO;
    
    private ProcessState _currentState = ProcessState.Idle;
    private ProcessRecipe _recipe = new();
    private CancellationTokenSource? _processCts;
    private DateTime _stageStartTime;
    private int _currentHighVacuumStage = 0;
    
    // 模擬壓力值 (實機需從感測器讀取)
    private double _currentPressure = 101325; // 大氣壓 Pa
    
    public ProcessState CurrentState => _currentState;
    public ProcessRecipe CurrentRecipe => _recipe;
    public double CurrentPressure => _currentPressure;
    public int CurrentHighVacuumStage => _currentHighVacuumStage;
    public bool IsRunning => _currentState != ProcessState.Idle && 
                             _currentState != ProcessState.Complete && 
                             _currentState != ProcessState.Error;
    
    public event Action<ProcessState, string>? OnStateChanged;
    public event Action<string>? OnProcessLog;
    public event Action<int, string>? OnAlarm;

    public VacuumProcessController(DualZController motionController, IDigitalIO? digitalIO = null)
    {
        _motionController = motionController;
        _digitalIO = digitalIO;
    }

    public void LoadRecipe(ProcessRecipe recipe)
    {
        _recipe = recipe;
        Log($"載入製程: {recipe.Name}");
    }

    /// <summary>
    /// 開始自動流程
    /// </summary>
    public async Task<bool> StartAsync()
    {
        if (IsRunning) return false;
        
        _processCts = new CancellationTokenSource();
        
        try
        {
            Log("開始自動流程");
            
            // 1. 關腔
            await CloseChamberAsync(_processCts.Token);
            
            // 2. 粗抽
            await RoughVacuumAsync(_processCts.Token);
            
            // 3. 細抽五段
            for (int i = 0; i < 5; i++)
            {
                await HighVacuumStageAsync(i, _processCts.Token);
            }
            
            // 4. 持壓
            await HoldPressureAsync(_processCts.Token);
            
            // 5. 破真空
            await VacuumBreakAsync(_processCts.Token);
            
            // 6. 開腔
            await OpenChamberAsync(_processCts.Token);
            
            SetState(ProcessState.Complete, "流程完成");
            return true;
        }
        catch (OperationCanceledException)
        {
            Log("流程已取消");
            return false;
        }
        catch (Exception ex)
        {
            SetState(ProcessState.Error, $"流程異常: {ex.Message}");
            RaiseAlarm(-1, ex.Message);
            return false;
        }
    }

    /// <summary>
    /// 暫停流程
    /// </summary>
    public void Pause()
    {
        if (IsRunning && _currentState != ProcessState.Paused)
        {
            SetState(ProcessState.Paused, "流程暫停");
        }
    }

    /// <summary>
    /// 繼續流程
    /// </summary>
    public void Resume()
    {
        if (_currentState == ProcessState.Paused)
        {
            Log("流程繼續");
        }
    }

    /// <summary>
    /// 停止流程
    /// </summary>
    public async Task StopAsync()
    {
        _processCts?.Cancel();
        await _motionController.EmergencyStopAsync();
        SetState(ProcessState.Idle, "流程停止");
    }

    // ===== 流程步驟 =====

    private async Task CloseChamberAsync(CancellationToken ct)
    {
        SetState(ProcessState.ClosingChamber, "關腔中...");
        
        // 移動到減速位置
        await _motionController.MoveSyncAsync(_recipe.ChamberSlowdownPosition, 100, ct);
        
        // 低速移動到關腔位置
        await _motionController.MoveSyncAsync(_recipe.ChamberClosePosition, 20, ct);
        
        Log($"關腔完成，位置: {_motionController.PositionZ1:F2} mm");
    }

    private async Task OpenChamberAsync(CancellationToken ct)
    {
        SetState(ProcessState.OpeningChamber, "開腔中...");
        
        // 移動到開腔位置
        await _motionController.MoveSyncAsync(_recipe.ChamberOpenPosition, 100, ct);
        
        Log($"開腔完成，位置: {_motionController.PositionZ1:F2} mm");
    }

    private async Task RoughVacuumAsync(CancellationToken ct)
    {
        SetState(ProcessState.RoughVacuum, "粗抽中...");
        _stageStartTime = DateTime.Now;
        
        // 開啟粗抽閥 (DO)
        _digitalIO?.WriteOutput(0, true);  // 粗抽閥
        
        // 等待壓力達標或逾時
        while (_currentPressure > _recipe.RoughVacuumTargetPressure)
        {
            ct.ThrowIfCancellationRequested();
            await WaitIfPaused(ct);
            
            // 模擬壓力下降
            _currentPressure *= 0.9;
            
            // 檢查逾時
            if ((DateTime.Now - _stageStartTime).TotalSeconds > _recipe.RoughVacuumTimeout)
            {
                RaiseAlarm(-110401, "粗抽逾時!!");
                throw new TimeoutException("粗抽逾時");
            }
            
            await Task.Delay(500, ct);
        }
        
        // 關閉粗抽閥
        _digitalIO?.WriteOutput(0, false);
        
        Log($"粗抽完成，壓力: {_currentPressure:F2} Pa");
    }

    private async Task HighVacuumStageAsync(int stageIndex, CancellationToken ct)
    {
        _currentHighVacuumStage = stageIndex + 1;
        var stage = _recipe.HighVacuumStages[stageIndex];
        
        var stateName = stageIndex switch
        {
            0 => ProcessState.HighVacuumStage1,
            1 => ProcessState.HighVacuumStage2,
            2 => ProcessState.HighVacuumStage3,
            3 => ProcessState.HighVacuumStage4,
            _ => ProcessState.HighVacuumStage5
        };
        
        SetState(stateName, $"細抽第 {stageIndex + 1} 段...");
        _stageStartTime = DateTime.Now;
        
        // 開啟細抽閥
        _digitalIO?.WriteOutput(1, true);  // 細抽閥
        
        // 設定蝶閥角度
        await _motionController.SetValveAsync(stage.ValveAngle, 30);
        Log($"蝶閥角度設定: {stage.ValveAngle}°");
        
        // 等待壓力達標或持續時間結束
        var endTime = DateTime.Now.AddSeconds(stage.Duration);
        while (DateTime.Now < endTime && _currentPressure > stage.StartPressure)
        {
            ct.ThrowIfCancellationRequested();
            await WaitIfPaused(ct);
            
            // 模擬壓力下降
            _currentPressure *= 0.95;
            
            // 檢查逾時
            if ((DateTime.Now - _stageStartTime).TotalSeconds > stage.Timeout)
            {
                RaiseAlarm(-110403 - stageIndex, $"細抽第{stageIndex + 1}段逾時!!");
                throw new TimeoutException($"細抽第{stageIndex + 1}段逾時");
            }
            
            await Task.Delay(500, ct);
        }
        
        Log($"細抽第 {stageIndex + 1} 段完成，壓力: {_currentPressure:F4} Pa");
    }

    private async Task HoldPressureAsync(CancellationToken ct)
    {
        SetState(ProcessState.HoldPressure, "持壓中...");
        
        var endTime = DateTime.Now.AddSeconds(_recipe.HoldPressureDuration);
        while (DateTime.Now < endTime)
        {
            ct.ThrowIfCancellationRequested();
            await WaitIfPaused(ct);
            await Task.Delay(500, ct);
        }
        
        Log($"持壓完成，持續 {_recipe.HoldPressureDuration} 秒");
    }

    private async Task VacuumBreakAsync(CancellationToken ct)
    {
        SetState(ProcessState.VacuumBreak, "破真空中...");
        _stageStartTime = DateTime.Now;
        
        // 關閉細抽閥
        _digitalIO?.WriteOutput(1, false);
        
        // 開啟破真空小閥
        _digitalIO?.WriteOutput(2, true);
        
        // 等待壓力上升
        while (_currentPressure < 50000)  // 500 mbar
        {
            ct.ThrowIfCancellationRequested();
            _currentPressure *= 1.5;
            
            if ((DateTime.Now - _stageStartTime).TotalSeconds > _recipe.VacuumBreakSmallTimeout)
            {
                // 開啟破真空大閥
                _digitalIO?.WriteOutput(3, true);
                break;
            }
            
            await Task.Delay(500, ct);
        }
        
        // 等待壓力接近大氣壓
        while (_currentPressure < 90000)
        {
            ct.ThrowIfCancellationRequested();
            _currentPressure *= 1.2;
            
            if ((DateTime.Now - _stageStartTime).TotalSeconds > _recipe.VacuumBreakLargeTimeout)
            {
                RaiseAlarm(-110503, "破真空大逾時!!");
                throw new TimeoutException("破真空大逾時");
            }
            
            await Task.Delay(500, ct);
        }
        
        // 關閉所有破真空閥
        _digitalIO?.WriteOutput(2, false);
        _digitalIO?.WriteOutput(3, false);
        
        _currentPressure = 101325;
        Log("破真空完成");
    }

    // ===== 輔助方法 =====

    private async Task WaitIfPaused(CancellationToken ct)
    {
        while (_currentState == ProcessState.Paused)
        {
            ct.ThrowIfCancellationRequested();
            await Task.Delay(100, ct);
        }
    }

    private void SetState(ProcessState state, string message)
    {
        _currentState = state;
        Log(message);
        OnStateChanged?.Invoke(state, message);
    }

    private void Log(string message)
    {
        OnProcessLog?.Invoke($"[{DateTime.Now:HH:mm:ss}] {message}");
    }

    private void RaiseAlarm(int code, string message)
    {
        OnAlarm?.Invoke(code, message);
        Log($"⚠️ 異常 {code}: {message}");
    }
}
