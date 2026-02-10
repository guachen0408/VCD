namespace VacuumDryer.Core.Process.Engine;

/// <summary>
/// 流程步驟介面 - 插件式設計
/// 每個步驟實作此介面即可被流程引擎執行
/// </summary>
public interface IProcessStep
{
    /// <summary>步驟名稱</summary>
    string Name { get; }
    
    /// <summary>步驟描述</summary>
    string Description { get; }
    
    /// <summary>
    /// 執行步驟
    /// </summary>
    Task ExecuteAsync(IProcessContext context, CancellationToken ct);
    
    /// <summary>
    /// 前置條件檢查 (可選，預設 true)
    /// </summary>
    bool CanExecute(IProcessContext context) => true;
}
