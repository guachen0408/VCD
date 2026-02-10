namespace VacuumDryer.Core.Process;

/// <summary>
/// 流程旗標結構 - 用於追蹤各步驟完成狀態
/// </summary>
public class ProcessFlags
{
    // ===== 動作完成旗標 =====
    public bool ChamberClosed { get; set; }
    public bool RoughVacuumDone { get; set; }
    public bool HighVacuum1Done { get; set; }
    public bool HighVacuum2Done { get; set; }
    public bool HighVacuum3Done { get; set; }
    public bool HighVacuum4Done { get; set; }
    public bool HighVacuum5Done { get; set; }
    public bool HoldPressureDone { get; set; }
    public bool VacuumBreakDone { get; set; }
    public bool ChamberOpened { get; set; }
    
    // ===== 狀態旗標 =====
    public bool IsRunning { get; set; }
    public bool IsPaused { get; set; }
    public bool HasError { get; set; }
    
    /// <summary>
    /// 重置所有旗標
    /// </summary>
    public void Reset()
    {
        ChamberClosed = false;
        RoughVacuumDone = false;
        HighVacuum1Done = false;
        HighVacuum2Done = false;
        HighVacuum3Done = false;
        HighVacuum4Done = false;
        HighVacuum5Done = false;
        HoldPressureDone = false;
        VacuumBreakDone = false;
        ChamberOpened = false;
        
        IsRunning = false;
        IsPaused = false;
        HasError = false;
    }
    
    /// <summary>
    /// 取得目前步驟索引 (0-10)
    /// </summary>
    public int CurrentStepIndex
    {
        get
        {
            if (!ChamberClosed) return 0;
            if (!RoughVacuumDone) return 1;
            if (!HighVacuum1Done) return 2;
            if (!HighVacuum2Done) return 3;
            if (!HighVacuum3Done) return 4;
            if (!HighVacuum4Done) return 5;
            if (!HighVacuum5Done) return 6;
            if (!HoldPressureDone) return 7;
            if (!VacuumBreakDone) return 8;
            if (!ChamberOpened) return 9;
            return 10; // 完成
        }
    }
}
