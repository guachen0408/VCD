using System.Text;

namespace VacuumDryer.Core.Data;

public record MotionDataRecord
{
    public DateTime Timestamp { get; init; }
    public double Z1Position { get; init; }
    public double Z2Position { get; init; }
    public double ValvePosition { get; init; }
    public double Z1Velocity { get; init; }
    public double Z2Velocity { get; init; }
    public double ValveVelocity { get; init; }
    public double Z1Torque { get; init; }
    public double Z2Torque { get; init; }
    public double ValveTorque { get; init; }
    public double SyncError { get; init; }
}

public class DataLogger : IDisposable
{
    private readonly List<MotionDataRecord> _records = new();
    private readonly object _lock = new();
    private readonly string _logDirectory;
    private bool _isRecording;
    private Timer? _recordTimer;

    public int RecordIntervalMs { get; set; } = 100;
    public bool IsRecording => _isRecording;
    public int RecordCount => _records.Count;
    public event Action<MotionDataRecord>? OnDataUpdated;

    public DataLogger(string logDirectory = "Logs")
    {
        _logDirectory = logDirectory;
        if (!Directory.Exists(_logDirectory))
            Directory.CreateDirectory(_logDirectory);
    }

    public void StartRecording(Func<MotionDataRecord> dataProvider)
    {
        if (_isRecording) return;
        
        _isRecording = true;
        _records.Clear();
        
        _recordTimer = new Timer(_ =>
        {
            try
            {
                var record = dataProvider();
                lock (_lock) { _records.Add(record); }
                OnDataUpdated?.Invoke(record);
            }
            catch { }
        }, null, 0, RecordIntervalMs);
    }

    public void StopRecording()
    {
        _isRecording = false;
        _recordTimer?.Dispose();
        _recordTimer = null;
    }

    public void AddRecord(MotionDataRecord record)
    {
        lock (_lock) { _records.Add(record); }
        OnDataUpdated?.Invoke(record);
    }

    public string ExportToCsv(string? fileName = null)
    {
        fileName ??= $"MotionData_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
        var filePath = Path.Combine(_logDirectory, fileName);
        
        var sb = new StringBuilder();
        sb.AppendLine("Timestamp,Z1_Pos,Z2_Pos,Valve_Pos,Z1_Vel,Z2_Vel,Valve_Vel,Z1_Torque,Z2_Torque,Valve_Torque,SyncError");
        
        lock (_lock)
        {
            foreach (var record in _records)
            {
                sb.AppendLine($"{record.Timestamp:yyyy-MM-dd HH:mm:ss.fff}," +
                             $"{record.Z1Position:F3},{record.Z2Position:F3},{record.ValvePosition:F3}," +
                             $"{record.Z1Velocity:F3},{record.Z2Velocity:F3},{record.ValveVelocity:F3}," +
                             $"{record.Z1Torque:F1},{record.Z2Torque:F1},{record.ValveTorque:F1}," +
                             $"{record.SyncError:F3}");
            }
        }
        
        File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        return filePath;
    }

    public IEnumerable<MotionDataRecord> GetRecentRecords(int count)
    {
        lock (_lock) { return _records.TakeLast(count).ToList(); }
    }

    public void ClearRecords()
    {
        lock (_lock) { _records.Clear(); }
    }

    public void Dispose() => StopRecording();
}
