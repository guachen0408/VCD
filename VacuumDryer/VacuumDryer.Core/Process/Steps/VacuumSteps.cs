using VacuumDryer.Core.Motion;
using VacuumDryer.Core.Process.Engine;
using VacuumDryer.Hardware;

namespace VacuumDryer.Core.Process.Steps;

// ===== 關腔步驟 =====

public class CloseChamberStep : IProcessStep
{
    public string Name => "CloseChamber";
    public string Description => "關腔";
    
    public async Task ExecuteAsync(IProcessContext context, CancellationToken ct)
    {
        var ctx = (VacuumProcessContext)context;
        var motion = context.GetService<DualZController>()!;
        
        // 移動到減速位置
        await motion.MoveSyncAsync(ctx.Recipe.ChamberSlowdownPosition, 100, ct);
        
        // 低速移動到關腔位置
        await motion.MoveSyncAsync(ctx.Recipe.ChamberClosePosition, 20, ct);
        
        context.Log($"關腔完成，位置: {motion.PositionZ1:F2} mm");
    }
}

// ===== 開腔步驟 =====

public class OpenChamberStep : IProcessStep
{
    public string Name => "OpenChamber";
    public string Description => "開腔";
    
    public async Task ExecuteAsync(IProcessContext context, CancellationToken ct)
    {
        var ctx = (VacuumProcessContext)context;
        var motion = context.GetService<DualZController>()!;
        
        await motion.MoveSyncAsync(ctx.Recipe.ChamberOpenPosition, 100, ct);
        
        context.Log($"開腔完成，位置: {motion.PositionZ1:F2} mm");
    }
}

// ===== 粗抽步驟 =====

public class RoughVacuumStep : IProcessStep
{
    public string Name => "RoughVacuum";
    public string Description => "粗抽";
    
    public async Task ExecuteAsync(IProcessContext context, CancellationToken ct)
    {
        var ctx = (VacuumProcessContext)context;
        var io = context.GetService<IDigitalIO>();
        
        ctx.StageStartTime = DateTime.Now;
        
        // 開啟粗抽閥
        io?.WriteOutput(0, true);
        
        // 等待壓力達標或逾時
        while (ctx.CurrentPressure > ctx.Recipe.RoughVacuumTargetPressure)
        {
            ct.ThrowIfCancellationRequested();
            
            // 模擬壓力下降
            ctx.CurrentPressure *= 0.9;
            
            // 檢查逾時
            if ((DateTime.Now - ctx.StageStartTime).TotalSeconds > ctx.Recipe.RoughVacuumTimeout)
            {
                context.RaiseAlarm(-110401, "粗抽逾時!!");
                throw new TimeoutException("粗抽逾時");
            }
            
            await Task.Delay(500, ct);
        }
        
        // 關閉粗抽閥
        io?.WriteOutput(0, false);
        
        context.Log($"粗抽完成，壓力: {ctx.CurrentPressure:F2} Pa");
    }
}

// ===== 細抽步驟 =====

public class HighVacuumStep : IProcessStep
{
    private readonly int _stageIndex;
    
    public string Name => $"HighVacuum{_stageIndex + 1}";
    public string Description => $"細抽第{_stageIndex + 1}段";
    
    public HighVacuumStep(int stageIndex)
    {
        _stageIndex = stageIndex;
    }
    
    public async Task ExecuteAsync(IProcessContext context, CancellationToken ct)
    {
        var ctx = (VacuumProcessContext)context;
        var io = context.GetService<IDigitalIO>();
        var motion = context.GetService<DualZController>()!;
        var stage = ctx.Recipe.HighVacuumStages[_stageIndex];
        
        ctx.CurrentHighVacuumStage = _stageIndex + 1;
        ctx.StageStartTime = DateTime.Now;
        
        // 開啟細抽閥
        io?.WriteOutput(1, true);
        
        // 設定蝶閥角度
        await motion.SetValveAsync(stage.ValveAngle, 30);
        context.Log($"蝶閥角度設定: {stage.ValveAngle}°");
        
        // 等待壓力達標或持續時間結束
        var endTime = DateTime.Now.AddSeconds(stage.Duration);
        while (DateTime.Now < endTime && ctx.CurrentPressure > stage.StartPressure)
        {
            ct.ThrowIfCancellationRequested();
            
            // 模擬壓力下降
            ctx.CurrentPressure *= 0.95;
            
            // 檢查逾時
            if ((DateTime.Now - ctx.StageStartTime).TotalSeconds > stage.Timeout)
            {
                context.RaiseAlarm(-110403 - _stageIndex, $"細抽第{_stageIndex + 1}段逾時!!");
                throw new TimeoutException($"細抽第{_stageIndex + 1}段逾時");
            }
            
            await Task.Delay(500, ct);
        }
        
        context.Log($"細抽第 {_stageIndex + 1} 段完成，壓力: {ctx.CurrentPressure:F4} Pa");
    }
}

// ===== 持壓步驟 =====

public class HoldPressureStep : IProcessStep
{
    public string Name => "HoldPressure";
    public string Description => "持壓";
    
    public async Task ExecuteAsync(IProcessContext context, CancellationToken ct)
    {
        var ctx = (VacuumProcessContext)context;
        
        var endTime = DateTime.Now.AddSeconds(ctx.Recipe.HoldPressureDuration);
        while (DateTime.Now < endTime)
        {
            ct.ThrowIfCancellationRequested();
            await Task.Delay(500, ct);
        }
        
        context.Log($"持壓完成，持續 {ctx.Recipe.HoldPressureDuration} 秒");
    }
}

// ===== 破真空步驟 =====

public class VacuumBreakStep : IProcessStep
{
    public string Name => "VacuumBreak";
    public string Description => "破真空";
    
    public async Task ExecuteAsync(IProcessContext context, CancellationToken ct)
    {
        var ctx = (VacuumProcessContext)context;
        var io = context.GetService<IDigitalIO>();
        
        ctx.StageStartTime = DateTime.Now;
        
        // 關閉細抽閥
        io?.WriteOutput(1, false);
        
        // 開啟破真空小閥
        io?.WriteOutput(2, true);
        
        // 等待壓力上升
        while (ctx.CurrentPressure < 50000)
        {
            ct.ThrowIfCancellationRequested();
            ctx.CurrentPressure *= 1.5;
            
            if ((DateTime.Now - ctx.StageStartTime).TotalSeconds > ctx.Recipe.VacuumBreakSmallValveTimeout)
            {
                io?.WriteOutput(3, true);
                break;
            }
            
            await Task.Delay(500, ct);
        }
        
        // 等待壓力接近大氣壓
        while (ctx.CurrentPressure < 90000)
        {
            ct.ThrowIfCancellationRequested();
            ctx.CurrentPressure *= 1.2;
            
            if ((DateTime.Now - ctx.StageStartTime).TotalSeconds > ctx.Recipe.VacuumBreakTimeout)
            {
                context.RaiseAlarm(-110503, "破真空大逾時!!");
                throw new TimeoutException("破真空大逾時");
            }
            
            await Task.Delay(500, ct);
        }
        
        // 關閉所有破真空閥
        io?.WriteOutput(2, false);
        io?.WriteOutput(3, false);
        
        ctx.CurrentPressure = 101325;
        context.Log("破真空完成");
    }
}
