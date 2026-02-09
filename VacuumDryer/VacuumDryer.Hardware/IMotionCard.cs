namespace VacuumDryer.Hardware;

/// <summary>
/// 數位 IO 介面
/// </summary>
public interface IDigitalIO
{
    /// <summary>讀取輸入通道</summary>
    bool ReadInput(int channel);
    
    /// <summary>讀取所有輸入</summary>
    bool[] ReadAllInputs();
    
    /// <summary>寫入輸出通道</summary>
    void WriteOutput(int channel, bool value);
    
    /// <summary>讀取輸出狀態</summary>
    bool ReadOutput(int channel);
    
    /// <summary>讀取所有輸出</summary>
    bool[] ReadAllOutputs();
}

/// <summary>
/// 運動控制卡介面
/// </summary>
public interface IMotionCard : IDisposable
{
    /// <summary>控制卡名稱</summary>
    string Name { get; }
    
    /// <summary>是否已連線</summary>
    bool IsConnected { get; }
    
    /// <summary>軸數量</summary>
    int AxisCount { get; }
    
    /// <summary>初始化控制卡</summary>
    Task<bool> InitializeAsync();
    
    /// <summary>取得指定軸</summary>
    IAxis GetAxis(int axisId);
    
    /// <summary>取得所有軸</summary>
    IEnumerable<IAxis> GetAllAxes();
    
    /// <summary>取得數位 IO</summary>
    IDigitalIO? GetDigitalIO();
    
    /// <summary>緊急停止所有軸</summary>
    Task EmergencyStopAllAsync();
    
    /// <summary>關閉控制卡</summary>
    Task CloseAsync();
}
