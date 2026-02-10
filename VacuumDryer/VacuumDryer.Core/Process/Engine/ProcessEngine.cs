namespace VacuumDryer.Core.Process.Engine;

/// <summary>
/// 通用流程引擎 - 負責樹遍歷、主迴圈、暫停/繼續/停止控制
/// 完全不依賴專案特定類型，可在任何專案中使用
/// </summary>
public class ProcessEngine
{
    private ProcessNode? _root;
    private ProcessNode? _current;
    private CancellationTokenSource? _cts;
    
    /// <summary>目前執行的節點</summary>
    public ProcessNode? CurrentNode => _current;
    
    /// <summary>流程樹根節點</summary>
    public ProcessNode? RootNode => _root;
    
    /// <summary>主迴圈間隔 (毫秒)</summary>
    public int LoopIntervalMs { get; set; } = 50;
    
    // ===== 事件 =====
    
    /// <summary>狀態變更事件 (state, message)</summary>
    public event Action<string, string>? OnStateChanged;
    
    /// <summary>日誌事件</summary>
    public event Action<string>? OnLog;
    
    /// <summary>步驟完成事件 (nodeId)</summary>
    public event Action<string>? OnStepCompleted;
    
    /// <summary>流程完成事件 (success)</summary>
    public event Action<bool>? OnProcessCompleted;
    
    /// <summary>
    /// 設定流程樹
    /// </summary>
    public void SetProcessTree(ProcessNode root)
    {
        _root = root;
    }
    
    /// <summary>
    /// 執行流程 (主迴圈)
    /// </summary>
    public async Task<bool> RunAsync(IProcessContext context)
    {
        if (context.IsRunning || _root == null) return false;
        
        context.ResetFlags();
        context.IsRunning = true;
        context.HasError = false;
        context.IsPaused = false;
        _cts = new CancellationTokenSource();
        
        OnLog?.Invoke("流程開始");
        
        try
        {
            while (context.IsRunning && !context.HasError)
            {
                _cts.Token.ThrowIfCancellationRequested();
                
                // 暫停處理
                while (context.IsPaused)
                {
                    _cts.Token.ThrowIfCancellationRequested();
                    await Task.Delay(100);
                }
                
                // 找到下一個要執行的步驟
                _current = FindNextStep(_root, context);
                
                if (_current == null)
                {
                    // 所有步驟完成
                    context.IsRunning = false;
                    context.SetState("Complete", "流程完成");
                    OnStateChanged?.Invoke("Complete", "流程完成");
                    OnProcessCompleted?.Invoke(true);
                    break;
                }
                
                // 執行當前步驟
                await ExecuteNodeAsync(_current, context, _cts.Token);
                
                await Task.Delay(LoopIntervalMs);
            }
            
            return !context.HasError;
        }
        catch (OperationCanceledException)
        {
            OnLog?.Invoke("流程已取消");
            context.IsRunning = false;
            OnProcessCompleted?.Invoke(false);
            return false;
        }
        catch (Exception ex)
        {
            context.HasError = true;
            context.IsRunning = false;
            context.SetState("Error", $"流程異常: {ex.Message}");
            OnStateChanged?.Invoke("Error", ex.Message);
            OnProcessCompleted?.Invoke(false);
            return false;
        }
    }
    
    /// <summary>
    /// 暫停流程
    /// </summary>
    public void Pause(IProcessContext context)
    {
        if (context.IsRunning && !context.IsPaused)
        {
            context.IsPaused = true;
            context.SetState("Paused", "流程暫停");
            OnStateChanged?.Invoke("Paused", "流程暫停");
            OnLog?.Invoke("流程暫停");
        }
    }
    
    /// <summary>
    /// 繼續流程
    /// </summary>
    public void Resume(IProcessContext context)
    {
        if (context.IsPaused)
        {
            context.IsPaused = false;
            OnLog?.Invoke("流程繼續");
        }
    }
    
    /// <summary>
    /// 停止流程
    /// </summary>
    public void Stop(IProcessContext context)
    {
        _cts?.Cancel();
        context.ResetFlags();
        context.IsRunning = false;
        context.SetState("Idle", "流程停止");
        OnStateChanged?.Invoke("Idle", "流程停止");
        OnLog?.Invoke("流程停止");
    }
    
    /// <summary>
    /// 跳過當前步驟
    /// </summary>
    public void SkipCurrentStep(IProcessContext context)
    {
        if (_current != null)
        {
            OnLog?.Invoke($"跳過步驟: {_current.Id}");
            _current.MarkComplete(context);
        }
    }
    
    // ===== 內部方法 =====
    
    /// <summary>
    /// 深度優先搜尋找到下一個未完成的步驟
    /// </summary>
    private ProcessNode? FindNextStep(ProcessNode? node, IProcessContext ctx)
    {
        if (node == null) return null;
        
        // 如果當前節點未完成且有步驟實作，返回它
        if (!node.IsCompleted(ctx) && node.Step != null)
        {
            // 檢查前置條件
            if (node.Step.CanExecute(ctx))
            {
                return node;
            }
        }
        
        // 遍歷子節點
        foreach (var child in node.Children)
        {
            var next = FindNextStep(child, ctx);
            if (next != null) return next;
        }
        
        return null;
    }
    
    /// <summary>
    /// 執行節點
    /// </summary>
    private async Task ExecuteNodeAsync(ProcessNode node, IProcessContext context, CancellationToken ct)
    {
        if (node.Step == null) return;
        
        // 設定狀態
        if (node.StateLabel != null)
        {
            context.SetState(node.StateLabel, node.Message ?? node.Id);
            OnStateChanged?.Invoke(node.StateLabel, node.Message ?? node.Id);
        }
        
        OnLog?.Invoke($"執行步驟: {node.Id} - {node.Message ?? node.Step.Description}");
        
        // 執行動作
        await node.Step.ExecuteAsync(context, ct);
        
        // 標記完成
        node.MarkComplete(context);
        OnStepCompleted?.Invoke(node.Id);
    }
}
