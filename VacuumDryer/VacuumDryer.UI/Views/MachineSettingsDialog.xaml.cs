using System.Windows;
using System.IO;
using System.Text.Json;
using System.Linq;

namespace VacuumDryer.UI.Views;

public class MachineSettings
{
    // Z軸參數
    public double OpenPosition { get; set; } = 0;
    public double SlowdownPosition { get; set; } = 280;
    public double ClosePosition { get; set; } = 300;
    public double FastSpeed { get; set; } = 100;
    public double SlowSpeed { get; set; } = 20;
    
    // 蝶閥參數
    public double ValveSpeed { get; set; } = 30;
    public double ValveFullOpen { get; set; } = 90;
    
    // 運動控制參數
    public double Acceleration { get; set; } = 500;
    public double Deceleration { get; set; } = 500;
    public double SyncTolerance { get; set; } = 0.1;
    
    // IO 通道
    public int RoughValveDO { get; set; } = 0;
    public int HighValveDO { get; set; } = 1;
    public int SmallBreakValveDO { get; set; } = 2;
    public int LargeBreakValveDO { get; set; } = 3;
}

public partial class MachineSettingsDialog : Window
{
    public MachineSettings Settings { get; private set; }
    private const string SettingsPath = @"D:\Data\VCD\Settings";
    
    public MachineSettingsDialog(MachineSettings settings)
    {
        InitializeComponent();
        
        // Deep copy
        var options = new JsonSerializerOptions { IncludeFields = true };
        string json = JsonSerializer.Serialize(settings, options);
        Settings = JsonSerializer.Deserialize<MachineSettings>(json, options) ?? new MachineSettings();
        
        DataContext = Settings;
        
        LoadSettingsFileList();
        SettingsFileComboBox.Text = "Machine";
    }

    private void LoadSettingsFileList()
    {
        try
        {
            if (Directory.Exists(SettingsPath))
            {
                var files = Directory.GetFiles(SettingsPath, "*.json");
                SettingsFileComboBox.ItemsSource = files.Select(f => Path.GetFileNameWithoutExtension(f)).ToList();
            }
        }
        catch { }
    }

    private void LoadSettings_Click(object sender, RoutedEventArgs e)
    {
        string filename = SettingsFileComboBox.Text;
        if (string.IsNullOrWhiteSpace(filename)) return;
        
        if (!filename.EndsWith(".json")) filename += ".json";
        
        string fullPath = Path.Combine(SettingsPath, filename);
        if (File.Exists(fullPath))
        {
            try
            {
                string json = File.ReadAllText(fullPath);
                var loadedSettings = JsonSerializer.Deserialize<MachineSettings>(json);
                if (loadedSettings != null)
                {
                    Settings = loadedSettings;
                    DataContext = Settings;
                    MessageBox.Show("載入成功", "資訊", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"載入失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        else
        {
            MessageBox.Show("檔案不存在", "錯誤", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void SaveAsSettings_Click(object sender, RoutedEventArgs e)
    {
        string filename = SettingsFileComboBox.Text;
        if (string.IsNullOrWhiteSpace(filename))
        {
            MessageBox.Show("請輸入檔案名稱", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!filename.EndsWith(".json")) filename += ".json";
        
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(Settings, options);
            File.WriteAllText(Path.Combine(SettingsPath, filename), json);
            LoadSettingsFileList();
            MessageBox.Show("儲存成功", "資訊", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"儲存失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
