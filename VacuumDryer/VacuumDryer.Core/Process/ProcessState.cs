namespace VacuumDryer.Core.Process;

/// <summary>
/// VCD 真空乾燥流程狀態
/// </summary>
public enum ProcessState
{
    Idle,               // 待機
    Initializing,       // 初始化
    OpeningChamber,     // 開腔中
    ClosingChamber,     // 關腔中
    RoughVacuum,        // 粗抽
    HighVacuumStage1,   // 細抽第一段
    HighVacuumStage2,   // 細抽第二段
    HighVacuumStage3,   // 細抽第三段
    HighVacuumStage4,   // 細抽第四段
    HighVacuumStage5,   // 細抽第五段
    HoldPressure,       // 持壓
    VacuumBreak,        // 破真空
    Complete,           // 完成
    Error,              // 異常
    Paused              // 暫停
}

/// <summary>
/// 製程參數 - 對應手冊中的材料參數設定
/// </summary>
public class ProcessRecipe
{
    public string Name { get; set; } = "Default";
    
    // 粗抽參數
    public double RoughVacuumTargetPressure { get; set; } = 1000;  // Pa
    public int RoughVacuumTimeout { get; set; } = 60;  // 秒
    
    // 細抽五段參數 (起始壓力、持續時間、閥門角度、持壓模式、逾時報警)
    public HighVacuumStageParams[] HighVacuumStages { get; set; } = new HighVacuumStageParams[5]
    {
        new() { StartPressure = 1000, TargetPressure = 500,  Duration = 10, ValveAngle = 90, HoldPressure = false, Timeout = 30 },
        new() { StartPressure = 500,  TargetPressure = 200,  Duration = 10, ValveAngle = 70, HoldPressure = false, Timeout = 30 },
        new() { StartPressure = 200,  TargetPressure = 100,  Duration = 10, ValveAngle = 50, HoldPressure = false, Timeout = 30 },
        new() { StartPressure = 100,  TargetPressure = 50,   Duration = 10, ValveAngle = 30, HoldPressure = false, Timeout = 30 },
        new() { StartPressure = 50,   TargetPressure = 10,   Duration = 10, ValveAngle = 10, HoldPressure = true,  Timeout = 60 }
    };
    
    // 持壓參數
    public int HoldPressureDuration { get; set; } = 30;  // 秒
    public double HoldPressureTarget { get; set; } = 10;  // Pa
    
    // 破真空參數
    public int VacuumBreakSmallValveTimeout { get; set; } = 10;  // 破真空小閥逾時
    public int VacuumBreakTimeout { get; set; } = 30;  // 破真空總逾時
    
    // 龍門位置參數
    public double ChamberOpenPosition { get; set; } = 0;      // 開腔位置
    public double ChamberClosePosition { get; set; } = 300;   // 關腔位置
    public double ChamberSlowdownPosition { get; set; } = 280; // 減速位置
}

public class HighVacuumStageParams
{
    public double StartPressure { get; set; }   // 起始壓力 (Pa)
    public double TargetPressure { get; set; }  // 目標壓力 (Pa)
    public int Duration { get; set; }           // 持續時間 (秒)
    public double ValveAngle { get; set; }      // 閥門角度 (度)
    public bool HoldPressure { get; set; }      // 持壓模式
    public int Timeout { get; set; }            // 逾時報警 (秒)
}
