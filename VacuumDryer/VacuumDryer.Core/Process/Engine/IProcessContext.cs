namespace VacuumDryer.Core.Process.Engine;

/// <summary>
/// 流程執行環境介面 - 提供旗標、狀態、日誌等功能
/// 各專案可自行實作此介面以注入專案特定的資源
/// </summary>
public interface IProcessContext
{
    // ===== 動態旗標 =====
    
    /// <summary>步驟完成旗標 (key=步驟Id, value=是否完成)</summary>
    Dictionary<string, bool> Flags { get; }
    
    // ===== 狀態管理 =====
    
    /// <summary>目前狀態標籤</summary>
    string CurrentState { get; set; }
    
    /// <summary>流程是否執行中</summary>
    bool IsRunning { get; set; }
    
    /// <summary>流程是否暫停</summary>
    bool IsPaused { get; set; }
    
    /// <summary>流程是否異常</summary>
    bool HasError { get; set; }
    
    // ===== 日誌與報警 =====
    
    /// <summary>記錄日誌</summary>
    void Log(string message);
    
    /// <summary>觸發報警</summary>
    void RaiseAlarm(int code, string message);
    
    /// <summary>設定狀態</summary>
    void SetState(string state, string message);
    
    // ===== 資源存取 =====
    
    /// <summary>
    /// 取得專案特定的服務/資源 (如動作控制器、IO 等)
    /// </summary>
    T? GetService<T>() where T : class;
    
    /// <summary>
    /// 重置所有旗標
    /// </summary>
    void ResetFlags();
}
