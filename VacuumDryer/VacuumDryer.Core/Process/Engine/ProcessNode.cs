namespace VacuumDryer.Core.Process.Engine;

/// <summary>
/// 流程節點 - 樹狀結構的通用節點
/// 每個節點包含一個 IProcessStep 和子節點清單
/// </summary>
public class ProcessNode
{
    /// <summary>唯一識別碼 (同時作為旗標 key)</summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>步驟實作 (插件)</summary>
    public IProcessStep? Step { get; set; }
    
    /// <summary>狀態標籤 (取代專案專屬 enum)</summary>
    public string? StateLabel { get; set; }
    
    /// <summary>步驟訊息</summary>
    public string? Message { get; set; }
    
    /// <summary>子節點清單</summary>
    public List<ProcessNode> Children { get; } = new();
    
    /// <summary>
    /// 檢查是否完成 (從 context.Flags 讀取)
    /// </summary>
    public bool IsCompleted(IProcessContext ctx)
    {
        return ctx.Flags.TryGetValue(Id, out var completed) && completed;
    }
    
    /// <summary>
    /// 標記完成 (寫入 context.Flags)
    /// </summary>
    public void MarkComplete(IProcessContext ctx)
    {
        ctx.Flags[Id] = true;
    }
    
    /// <summary>
    /// 取得下一個未完成的子節點
    /// </summary>
    public ProcessNode? GetNextChild(IProcessContext ctx)
    {
        return Children.FirstOrDefault(c => !c.IsCompleted(ctx));
    }
    
    /// <summary>
    /// 檢查所有子節點是否完成
    /// </summary>
    public bool AllChildrenCompleted(IProcessContext ctx)
    {
        return Children.Count == 0 || Children.All(c => c.IsCompleted(ctx));
    }
    
    /// <summary>
    /// 加入子節點 (Fluent API)
    /// </summary>
    public ProcessNode AddChild(ProcessNode child)
    {
        Children.Add(child);
        return child; // 返回子節點以支援鏈式呼叫
    }
    
    /// <summary>
    /// 加入子節點 (快速建立)
    /// </summary>
    public ProcessNode AddChild(string id, IProcessStep step, string? stateLabel = null, string? message = null)
    {
        var child = new ProcessNode
        {
            Id = id,
            Step = step,
            StateLabel = stateLabel,
            Message = message ?? step.Description
        };
        Children.Add(child);
        return child;
    }
}
