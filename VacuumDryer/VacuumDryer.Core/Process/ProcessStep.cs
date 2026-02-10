namespace VacuumDryer.Core.Process;

/// <summary>
/// 流程步驟節點 - 樹狀流程結構
/// </summary>
public class ProcessStep
{
    public string Name { get; set; } = string.Empty;
    public Func<CancellationToken, Task>? Action { get; set; }
    public ProcessState? State { get; set; }
    public string? Message { get; set; }
    public List<ProcessStep> Children { get; } = new();
    
    /// <summary>
    /// 完成判斷謂詞 - 檢查旗標決定是否完成
    /// </summary>
    public Func<ProcessFlags, bool>? CompletionPredicate { get; set; }
    
    /// <summary>
    /// 完成時設定旗標的動作
    /// </summary>
    public Action<ProcessFlags>? SetCompleteFlag { get; set; }
    
    /// <summary>
    /// 檢查是否完成 (優先使用謂詞，否則使用內部狀態)
    /// </summary>
    public bool IsCompleted(ProcessFlags flags)
    {
        if (CompletionPredicate != null)
        {
            return CompletionPredicate(flags);
        }
        return _isCompleted;
    }
    
    private bool _isCompleted;
    
    /// <summary>
    /// 標記完成 (設定內部狀態並觸發旗標動作)
    /// </summary>
    public void MarkComplete(ProcessFlags flags)
    {
        _isCompleted = true;
        SetCompleteFlag?.Invoke(flags);
    }
    
    /// <summary>
    /// 取得下一個未完成的子節點
    /// </summary>
    public ProcessStep? GetNextChild(ProcessFlags flags)
    {
        return Children.FirstOrDefault(c => !c.IsCompleted(flags));
    }
    
    /// <summary>
    /// 重置此節點及所有子節點
    /// </summary>
    public void Reset()
    {
        _isCompleted = false;
        foreach (var child in Children)
        {
            child.Reset();
        }
    }
    
    /// <summary>
    /// 檢查所有子節點是否完成
    /// </summary>
    public bool AllChildrenCompleted(ProcessFlags flags) => 
        Children.Count == 0 || Children.All(c => c.IsCompleted(flags));
}
