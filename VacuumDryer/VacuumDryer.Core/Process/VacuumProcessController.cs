using VacuumDryer.Core.Motion;
using VacuumDryer.Core.Process.Engine;
using VacuumDryer.Core.Process.Steps;
using VacuumDryer.Hardware;

namespace VacuumDryer.Core.Process;

/// <summary>
/// VCD 真空乾燥流程控制器
/// 使用通用 ProcessEngine 框架，以插件方式註冊步驟
/// </summary>
public class VacuumProcessController
{
    private readonly ProcessEngine _engine = new();
    private readonly VacuumProcessContext _context = new();
    
    private ProcessState _currentState = ProcessState.Idle;
    
    public ProcessState CurrentState => _currentState;
    public ProcessRecipe CurrentRecipe => _context.Recipe;
    public double CurrentPressure => _context.CurrentPressure;
    public int CurrentHighVacuumStage => _context.CurrentHighVacuumStage;
    public bool IsRunning => _context.IsRunning;
    public bool IsPaused => _context.IsPaused;
    public DateTime StageStartTime => _context.StageStartTime;
    public VacuumProcessContext Context => _context;
    public ProcessEngine Engine => _engine;
    
    public event Action<ProcessState, string>? OnStateChanged;
    public event Action<string>? OnProcessLog;
    public event Action<int, string>? OnAlarm;

    public VacuumProcessController(DualZController motionController, IDigitalIO? digitalIO = null)
    {
        // 註冊服務到 Context
        _context.RegisterService(motionController);
        if (digitalIO != null)
        {
            _context.RegisterService(digitalIO);
        }
        
        // 綁定 Context 事件
        _context.OnStateChanged += (state, msg) => 
        {
            _currentState = ParseState(state);
            OnStateChanged?.Invoke(_currentState, msg);
        };
        _context.OnLog += msg => OnProcessLog?.Invoke(msg);
        _context.OnAlarm += (code, msg) => OnAlarm?.Invoke(code, msg);
        
        // 綁定 Engine 事件
        _engine.OnLog += msg => OnProcessLog?.Invoke(msg);
        _engine.OnStateChanged += (state, msg) =>
        {
            _currentState = ParseState(state);
            OnStateChanged?.Invoke(_currentState, msg);
        };
        
        // 建立預設流程樹
        _engine.SetProcessTree(BuildDefaultProcessTree());
    }

    public void LoadRecipe(ProcessRecipe recipe)
    {
        _context.Recipe = recipe;
        _context.Log($"載入製程: {recipe.Name}");
    }

    public void UpdateRecipe(ProcessRecipe recipe)
    {
        if (IsRunning)
        {
            _context.Log("⚠️ 流程運行中，部分參數可能在下一週期生效");
        }
        _context.Recipe = recipe;
        _context.Log("製程參數已更新");
    }

    /// <summary>
    /// 開始自動流程
    /// </summary>
    public async Task<bool> StartAsync()
    {
        _context.CurrentPressure = 101325;
        return await _engine.RunAsync(_context);
    }

    /// <summary>暫停流程</summary>
    public void Pause() => _engine.Pause(_context);

    /// <summary>繼續流程</summary>
    public void Resume() => _engine.Resume(_context);

    /// <summary>停止流程</summary>
    public async Task StopAsync()
    {
        _engine.Stop(_context);
        var motion = _context.GetService<DualZController>();
        if (motion != null)
        {
            await motion.EmergencyStopAsync();
        }
        _currentState = ProcessState.Idle;
    }

    /// <summary>
    /// 設定自訂流程樹 (可外部重組流程順序)
    /// </summary>
    public void SetProcessTree(ProcessNode root)
    {
        _engine.SetProcessTree(root);
    }

    /// <summary>
    /// 建立預設流程樹 (VacuumDryer 標準流程)
    /// </summary>
    public ProcessNode BuildDefaultProcessTree()
    {
        var root = new ProcessNode { Id = "Root" };
        
        // 使用 Fluent API 建立樹
        var closeChamber = root.AddChild("CloseChamber", new CloseChamberStep(), "ClosingChamber", "關腔中...");
        var roughVacuum = closeChamber.AddChild("RoughVacuum", new RoughVacuumStep(), "RoughVacuum", "粗抽中...");
        
        var highVac1 = roughVacuum.AddChild("HighVacuum1", new HighVacuumStep(0), "HighVacuumStage1", "細抽第1段...");
        var highVac2 = highVac1.AddChild("HighVacuum2", new HighVacuumStep(1), "HighVacuumStage2", "細抽第2段...");
        var highVac3 = highVac2.AddChild("HighVacuum3", new HighVacuumStep(2), "HighVacuumStage3", "細抽第3段...");
        var highVac4 = highVac3.AddChild("HighVacuum4", new HighVacuumStep(3), "HighVacuumStage4", "細抽第4段...");
        var highVac5 = highVac4.AddChild("HighVacuum5", new HighVacuumStep(4), "HighVacuumStage5", "細抽第5段...");
        
        var holdPressure = highVac5.AddChild("HoldPressure", new HoldPressureStep(), "HoldPressure", "持壓中...");
        var vacuumBreak = holdPressure.AddChild("VacuumBreak", new VacuumBreakStep(), "VacuumBreak", "破真空中...");
        vacuumBreak.AddChild("OpenChamber", new OpenChamberStep(), "OpeningChamber", "開腔中...");
        
        return root;
    }
    
    /// <summary>
    /// 將字串狀態轉換為 ProcessState enum (向後相容)
    /// </summary>
    private static ProcessState ParseState(string state)
    {
        return state switch
        {
            "Idle" => ProcessState.Idle,
            "ClosingChamber" => ProcessState.ClosingChamber,
            "OpeningChamber" => ProcessState.OpeningChamber,
            "RoughVacuum" => ProcessState.RoughVacuum,
            "HighVacuumStage1" => ProcessState.HighVacuumStage1,
            "HighVacuumStage2" => ProcessState.HighVacuumStage2,
            "HighVacuumStage3" => ProcessState.HighVacuumStage3,
            "HighVacuumStage4" => ProcessState.HighVacuumStage4,
            "HighVacuumStage5" => ProcessState.HighVacuumStage5,
            "HoldPressure" => ProcessState.HoldPressure,
            "VacuumBreak" => ProcessState.VacuumBreak,
            "Complete" => ProcessState.Complete,
            "Error" => ProcessState.Error,
            "Paused" => ProcessState.Paused,
            _ => ProcessState.Idle
        };
    }
}
