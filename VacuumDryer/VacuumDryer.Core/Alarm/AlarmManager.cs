using System.Text.Json;
using System.Text.Json.Serialization;

namespace VacuumDryer.Core.Alarm;

/// <summary>
/// 警報嚴重等級
/// </summary>
public enum AlarmSeverity
{
    Info,       // 提示
    Warning,    // 警告 (可繼續運行)
    Error,      // 錯誤 (需操作員處理)
    Critical    // 嚴重 (立即停機)
}

/// <summary>
/// 警報記錄
/// </summary>
public record AlarmRecord
{
    public int Code { get; init; }
    public string Message { get; init; } = string.Empty;
    public AlarmSeverity Severity { get; init; }
    public DateTime OccurredAt { get; init; } = DateTime.Now;
    public DateTime? AcknowledgedAt { get; set; }
    public DateTime? ClearedAt { get; set; }
    public string Source { get; init; } = string.Empty;
    
    [JsonIgnore]
    public bool IsActive => ClearedAt == null;
    
    [JsonIgnore]
    public bool IsAcknowledged => AcknowledgedAt != null;
}

/// <summary>
/// 警報管理器 - 管理警報佇列、歷史記錄、通知
/// </summary>
public class AlarmManager
{
    private readonly List<AlarmRecord> _activeAlarms = new();
    private readonly List<AlarmRecord> _history = new();
    private readonly object _lock = new();
    private readonly string _historyDirectory;
    
    /// <summary>最大歷史記錄數</summary>
    public int MaxHistoryCount { get; set; } = 10000;
    
    /// <summary>目前啟用的警報數量</summary>
    public int ActiveCount { get { lock (_lock) return _activeAlarms.Count; } }
    
    /// <summary>是否有嚴重警報</summary>
    public bool HasCritical { get { lock (_lock) return _activeAlarms.Any(a => a.Severity == AlarmSeverity.Critical); } }
    
    /// <summary>是否有錯誤警報</summary>
    public bool HasError { get { lock (_lock) return _activeAlarms.Any(a => a.Severity >= AlarmSeverity.Error); } }
    
    // ===== 事件 =====
    
    /// <summary>新警報觸發</summary>
    public event Action<AlarmRecord>? OnAlarmRaised;
    
    /// <summary>警報確認</summary>
    public event Action<AlarmRecord>? OnAlarmAcknowledged;
    
    /// <summary>警報清除</summary>
    public event Action<AlarmRecord>? OnAlarmCleared;
    
    public AlarmManager(string historyDirectory = "Logs/Alarms")
    {
        _historyDirectory = historyDirectory;
        if (!Directory.Exists(_historyDirectory))
            Directory.CreateDirectory(_historyDirectory);
    }
    
    /// <summary>
    /// 觸發警報
    /// </summary>
    public AlarmRecord Raise(int code, string message, AlarmSeverity severity = AlarmSeverity.Error, string source = "")
    {
        var alarm = new AlarmRecord
        {
            Code = code,
            Message = message,
            Severity = severity,
            OccurredAt = DateTime.Now,
            Source = source
        };
        
        lock (_lock)
        {
            // 避免重複觸發相同 code
            if (_activeAlarms.Any(a => a.Code == code))
                return _activeAlarms.First(a => a.Code == code);
            
            _activeAlarms.Add(alarm);
            _history.Add(alarm);
            
            // 限制歷史數量
            if (_history.Count > MaxHistoryCount)
                _history.RemoveRange(0, _history.Count - MaxHistoryCount);
        }
        
        OnAlarmRaised?.Invoke(alarm);
        return alarm;
    }
    
    /// <summary>
    /// 確認警報 (操作員已看到)
    /// </summary>
    public void Acknowledge(int code)
    {
        lock (_lock)
        {
            var alarm = _activeAlarms.FirstOrDefault(a => a.Code == code);
            if (alarm != null)
            {
                alarm.AcknowledgedAt = DateTime.Now;
                OnAlarmAcknowledged?.Invoke(alarm);
            }
        }
    }
    
    /// <summary>
    /// 確認所有警報
    /// </summary>
    public void AcknowledgeAll()
    {
        lock (_lock)
        {
            foreach (var alarm in _activeAlarms.Where(a => !a.IsAcknowledged))
            {
                alarm.AcknowledgedAt = DateTime.Now;
                OnAlarmAcknowledged?.Invoke(alarm);
            }
        }
    }
    
    /// <summary>
    /// 清除警報
    /// </summary>
    public void Clear(int code)
    {
        lock (_lock)
        {
            var alarm = _activeAlarms.FirstOrDefault(a => a.Code == code);
            if (alarm != null)
            {
                alarm.ClearedAt = DateTime.Now;
                _activeAlarms.Remove(alarm);
                OnAlarmCleared?.Invoke(alarm);
            }
        }
    }
    
    /// <summary>
    /// 清除所有警報
    /// </summary>
    public void ClearAll()
    {
        lock (_lock)
        {
            foreach (var alarm in _activeAlarms.ToList())
            {
                alarm.ClearedAt = DateTime.Now;
                _activeAlarms.Remove(alarm);
                OnAlarmCleared?.Invoke(alarm);
            }
        }
    }
    
    /// <summary>
    /// 取得所有啟用警報
    /// </summary>
    public IReadOnlyList<AlarmRecord> GetActiveAlarms()
    {
        lock (_lock) { return _activeAlarms.ToList().AsReadOnly(); }
    }
    
    /// <summary>
    /// 取得歷史記錄
    /// </summary>
    public IReadOnlyList<AlarmRecord> GetHistory(int count = 100)
    {
        lock (_lock) { return _history.TakeLast(count).Reverse().ToList().AsReadOnly(); }
    }
    
    /// <summary>
    /// 匯出歷史到 JSON
    /// </summary>
    public string ExportHistory(string? fileName = null)
    {
        fileName ??= $"AlarmHistory_{DateTime.Now:yyyyMMdd_HHmmss}.json";
        var filePath = Path.Combine(_historyDirectory, fileName);
        
        List<AlarmRecord> snapshot;
        lock (_lock) { snapshot = _history.ToList(); }
        
        var json = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions 
        { 
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });
        File.WriteAllText(filePath, json);
        
        return filePath;
    }
}
