using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using VacuumDryer.Core.Motion;
using VacuumDryer.Core.Process;
using VacuumDryer.Hardware;
using VacuumDryer.Hardware.Simulation;
using System.IO;
using System.Text.Json;

namespace VacuumDryer.UI.Views;

public partial class MainWindow : Window
{
    private readonly IMotionCard _motionCard;
    private readonly DualZController _controller;
    private readonly VacuumProcessController _processController;
    private readonly DispatcherTimer _updateTimer;

    public MainWindow()
    {
        InitializeComponent();
        
        InitStorage();
        
        // 使用模擬控制卡 (實機時換成 DeltaPciL221MotionCard)
        _motionCard = new SimulatedMotionCard3Axis();
        _motionCard.InitializeAsync().Wait();
        
        _controller = new DualZController(_motionCard);
        _processController = new VacuumProcessController(_controller, _motionCard.GetDigitalIO());
        
        // 訂閱流程事件
        _processController.OnStateChanged += ProcessController_OnStateChanged;
        _processController.OnProcessLog += ProcessController_OnProcessLog;
        _processController.OnAlarm += ProcessController_OnAlarm;

        LoadSettings(); // Moved here to avoid NullReferenceException
        
        _updateTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
        _updateTimer.Tick += UpdateTimer_Tick;
        _updateTimer.Start();
        
        AddLog("系統初始化完成");
        AddLog($"控制卡: {_motionCard.Name}");
    }



    private void ProcessController_OnProcessLog(string message)
    {
        Dispatcher.Invoke(() => AddLog(message));
    }

    private void ProcessController_OnAlarm(int code, string message)
    {
        Dispatcher.Invoke(() =>
        {
            MessageBox.Show($"異常代碼: {code}\n\n{message}", "設備異常", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
        });
    }

    private string GetStateDisplayName(ProcessState state) => state switch
    {
        ProcessState.Idle => "手動待機中",
        ProcessState.Initializing => "初始化中",
        ProcessState.OpeningChamber => "開腔中",
        ProcessState.ClosingChamber => "關腔中",
        ProcessState.RoughVacuum => "粗抽中",
        ProcessState.HighVacuumStage1 => "細抽第一段",
        ProcessState.HighVacuumStage2 => "細抽第二段",
        ProcessState.HighVacuumStage3 => "細抽第三段",
        ProcessState.HighVacuumStage4 => "細抽第四段",
        ProcessState.HighVacuumStage5 => "細抽第五段",
        ProcessState.HoldPressure => "持壓中",
        ProcessState.VacuumBreak => "破真空中",
        ProcessState.Complete => "流程完成",
        ProcessState.Error => "異常",
        ProcessState.Paused => "暫停中",
        _ => "未知狀態"
    };

    private void UpdateProcessStepIndicator(ProcessState state)
    {
        // 重置所有步驟顏色
        var gray = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0"));
        var green = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));
        var yellow = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFB300"));
        
        StepCloseChamber.Background = gray;
        StepRoughVacuum.Background = gray;
        StepHighVacuum1.Background = gray;
        StepHighVacuum2.Background = gray;
        StepHighVacuum3.Background = gray;
        StepHighVacuum4.Background = gray;
        StepHighVacuum5.Background = gray;
        StepHoldPressure.Background = gray;
        StepVacuumBreak.Background = gray;
        StepOpenChamber.Background = gray;

        // 設定當前步驟為黃色
        switch (state)
        {
            case ProcessState.ClosingChamber:
                StepCloseChamber.Background = yellow;
                break;
            case ProcessState.RoughVacuum:
                StepCloseChamber.Background = green;
                StepRoughVacuum.Background = yellow;
                break;
            case ProcessState.HighVacuumStage1:
                StepCloseChamber.Background = green;
                StepRoughVacuum.Background = green;
                StepHighVacuum1.Background = yellow;
                break;
            case ProcessState.HighVacuumStage2:
                StepCloseChamber.Background = green;
                StepRoughVacuum.Background = green;
                StepHighVacuum1.Background = green;
                StepHighVacuum2.Background = yellow;
                break;
            case ProcessState.HighVacuumStage3:
                StepCloseChamber.Background = green;
                StepRoughVacuum.Background = green;
                StepHighVacuum1.Background = green;
                StepHighVacuum2.Background = green;
                StepHighVacuum3.Background = yellow;
                break;
            case ProcessState.HighVacuumStage4:
                StepCloseChamber.Background = green;
                StepRoughVacuum.Background = green;
                StepHighVacuum1.Background = green;
                StepHighVacuum2.Background = green;
                StepHighVacuum3.Background = green;
                StepHighVacuum4.Background = yellow;
                break;
            case ProcessState.HighVacuumStage5:
                StepCloseChamber.Background = green;
                StepRoughVacuum.Background = green;
                StepHighVacuum1.Background = green;
                StepHighVacuum2.Background = green;
                StepHighVacuum3.Background = green;
                StepHighVacuum4.Background = green;
                StepHighVacuum5.Background = yellow;
                break;
            case ProcessState.HoldPressure:
                StepCloseChamber.Background = green;
                StepRoughVacuum.Background = green;
                StepHighVacuum1.Background = green;
                StepHighVacuum2.Background = green;
                StepHighVacuum3.Background = green;
                StepHighVacuum4.Background = green;
                StepHighVacuum5.Background = green;
                StepHoldPressure.Background = yellow;
                break;
            case ProcessState.VacuumBreak:
                StepCloseChamber.Background = green;
                StepRoughVacuum.Background = green;
                StepHighVacuum1.Background = green;
                StepHighVacuum2.Background = green;
                StepHighVacuum3.Background = green;
                StepHighVacuum4.Background = green;
                StepHighVacuum5.Background = green;
                StepHoldPressure.Background = green;
                StepVacuumBreak.Background = yellow;
                break;
            case ProcessState.OpeningChamber:
            case ProcessState.Complete:
                StepCloseChamber.Background = green;
                StepRoughVacuum.Background = green;
                StepHighVacuum1.Background = green;
                StepHighVacuum2.Background = green;
                StepHighVacuum3.Background = green;
                StepHighVacuum4.Background = green;
                StepHighVacuum5.Background = green;
                StepHoldPressure.Background = green;
                StepVacuumBreak.Background = green;
                StepOpenChamber.Background = state == ProcessState.OpeningChamber ? yellow : green;
                break;
        }
    }

    // ===== 分頁切換 =====
    private struct ChartDataPoint
    {
        public double TimeSeconds; // 相對時間
        public double Pressure;
        public double ValveAngle;
    }

    private readonly List<ChartDataPoint> _chartData = new();
    private DateTime? _processStartTime;

    private void UpdateTimer_Tick(object? sender, EventArgs e)
    {
        // 軸位置更新
        double valveAngle = _controller.ValvePosition;
        ValveAngle.Text = valveAngle.ToString("F1"); 
        ChamberPosition.Text = _controller.PositionZ1.ToString("F1");
        
        // 壓力更新
        double pressure = _processController.CurrentPressure;
        CurrentPressure.Text = pressure.ToString("F1");
        
        if (_processController.CurrentRecipe != null)
            TargetPressureText.Text = _processController.CurrentRecipe.RoughVacuumTargetPressure.ToString("F0");
        
        // 流程時間更新 & 圖表紀錄
        if (_processController.IsRunning)
        {
            if (_processStartTime == null)
            {
                _processStartTime = DateTime.Now;
                _chartData.Clear(); // 新流程開始清空圖表
            }

            var duration = DateTime.Now - _processStartTime.Value;
            StepTimeText.Text = duration.ToString(@"mm\:ss");

            // 紀錄數據
            _chartData.Add(new ChartDataPoint 
            { 
                TimeSeconds = duration.TotalSeconds,
                Pressure = pressure,
                ValveAngle = valveAngle
            });
        }
        else
        {
            StepTimeText.Text = "00:00";
            _processStartTime = null; // 重置開始時間標記
        }

        // 時間更新
        TimeText.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        if (ChartView.Visibility == Visibility.Visible)
        {
            DrawChart();
        }
    }

    private void DrawChart()
    {
        VacuumChartCanvas.Children.Clear();
        
        double w = VacuumChartCanvas.ActualWidth;
        double h = VacuumChartCanvas.ActualHeight;
        
        if (w <= 0 || h <= 0 || _chartData.Count < 2) return;
        
        // 1. 繪製網格
        for (int i = 0; i <= 5; i++)
        {
            double y = i * h / 5;
            VacuumChartCanvas.Children.Add(new System.Windows.Shapes.Line
            {
                X1 = 0, Y1 = y, X2 = w, Y2 = y,
                Stroke = Brushes.LightGray, StrokeThickness = 1, StrokeDashArray = new DoubleCollection { 2, 2 }
            });
        }

        // 2. 計算 X 軸 Scale (總時間)
        double maxTime = _chartData.Last().TimeSeconds;
        if (maxTime < 10) maxTime = 10; // 最小顯示範圍 10秒

        // 3. 繪製壓力曲線 (Blue, 0-102000 Pa) - 這裡簡化用 Linear，實際真空建議用 Log
        // 為了讓低真空變化明顯，這裡先把 Max 設為 102000，但如果都在低壓區，可以動態調整
        // 用戶需求是"過程"，通常包含 大氣 -> 真空。
        double maxPressure = 102000;
        
        var pressureLine = new System.Windows.Shapes.Polyline
        {
            Stroke = Brushes.DodgerBlue,
            StrokeThickness = 2
        };

        // 4. 繪製閥門角度曲線 (Orange, 0-90 度)
        var valveLine = new System.Windows.Shapes.Polyline
        {
            Stroke = Brushes.Orange,
            StrokeThickness = 2,
            StrokeDashArray = new DoubleCollection { 4, 2 } // 虛線區分
        };

        foreach (var p in _chartData)
        {
            double x = (p.TimeSeconds / maxTime) * w;
            
            // Pressure Y (Linear 0-102000)
            double py = h - (p.Pressure / maxPressure * h);
            pressureLine.Points.Add(new Point(x, py < 0 ? 0 : py)); // Clamp

            // Valve Y (0-90)
            double vy = h - (p.ValveAngle / 90.0 * h);
            valveLine.Points.Add(new Point(x, vy));
        }
        
        VacuumChartCanvas.Children.Add(pressureLine);
        VacuumChartCanvas.Children.Add(valveLine);

        // 5. 圖例 (Legend) - 直接畫在 Canvas 上
        var legendPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(5) };
        Canvas.SetRight(legendPanel, 10);
        Canvas.SetTop(legendPanel, 10);

        legendPanel.Children.Add(new TextBlock { Text = "── 壓力 (Pa)", Foreground = Brushes.DodgerBlue, FontWeight = FontWeights.Bold, Margin = new Thickness(0,0,10,0) });
        legendPanel.Children.Add(new TextBlock { Text = "--- 閥門 (°)", Foreground = Brushes.Orange, FontWeight = FontWeights.Bold });
        VacuumChartCanvas.Children.Add(legendPanel);
    }

    // ===== 流程控制器事件 =====
    
    private void ProcessController_OnStateChanged(ProcessState state, string message)
    {
        Dispatcher.Invoke(() =>
        {
            // 更新機台狀態顯示
            MachineStatus.Text = GetStateDisplayName(state);
            CurrentStepText.Text = GetStateDisplayName(state);
            
            // 更新步驟指示燈
            UpdateProcessStepIndicator(state);
            
            // 更新模式顯示
            if (state == ProcessState.Idle || state == ProcessState.Complete || state == ProcessState.Error)
            {
                AutoModeBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#90A4AE"));
            }
            else
            {
                AutoModeBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));
            }
        });
    }

    // ===== 分頁切換 =====
    private void TabMain_Click(object sender, MouseButtonEventArgs e)
    {
        TabMain.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));
        TabChart.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0"));
        TabAlarm.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0"));
        ((TextBlock)TabMain.Child).Foreground = Brushes.White;
        ((TextBlock)TabChart.Child).Foreground = Brushes.Black;
        ((TextBlock)TabAlarm.Child).Foreground = Brushes.Black;
        
        MainView.Visibility = Visibility.Visible;
        ChartView.Visibility = Visibility.Hidden;
    }

    private void TabChart_Click(object sender, MouseButtonEventArgs e)
    {
        TabMain.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0"));
        TabChart.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));
        TabAlarm.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0"));
        ((TextBlock)TabMain.Child).Foreground = Brushes.Black;
        ((TextBlock)TabChart.Child).Foreground = Brushes.White;
        ((TextBlock)TabAlarm.Child).Foreground = Brushes.Black;
        
        MainView.Visibility = Visibility.Hidden;
        ChartView.Visibility = Visibility.Visible;
        
        AddLog("切換至圖表頁面");
    }

    private void TabAlarm_Click(object sender, MouseButtonEventArgs e)
    {
        TabMain.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0"));
        TabChart.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0"));
        TabAlarm.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));
        ((TextBlock)TabMain.Child).Foreground = Brushes.Black;
        ((TextBlock)TabChart.Child).Foreground = Brushes.Black;
        ((TextBlock)TabAlarm.Child).Foreground = Brushes.White;
        
        MainView.Visibility = Visibility.Hidden;
        ChartView.Visibility = Visibility.Hidden;
        
        AddLog("切換至異常頁面");
        MessageBox.Show("異常歷史功能開發中...", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    // ===== 功能按鈕事件 =====

    private void Login_Click(object sender, RoutedEventArgs e)
    {
        AddLog("開啟登入介面");
        MessageBox.Show("登入功能\n\n權限等級:\n• Operator (操作員)\n• Engineer (工程師)\n• Supervisor (廠商)", 
            "登入", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        // 顯示子選單
        if (sender is Button button && button.ContextMenu != null)
        {
            button.ContextMenu.PlacementTarget = button;
            button.ContextMenu.IsOpen = true;
        }
    }

    private ProcessRecipe _currentRecipe = new();
    private MachineSettings _machineSettings = new();

    private void RecipeSettings_Click(object sender, RoutedEventArgs e)
    {
        AddLog("開啟製程參數設定");
        var dialog = new RecipeSettingsDialog(_currentRecipe);
        dialog.Owner = this;
        if (dialog.ShowDialog() == true)
        {
            _currentRecipe = dialog.Recipe;
            _processController.UpdateRecipe(_currentRecipe);
            SaveSettings(); // 儲存設定
            AddLog("製程參數已更新並儲存");
        }
    }

    private void MachineSettings_Click(object sender, RoutedEventArgs e)
    {
        AddLog("開啟機台參數設定");
        var dialog = new MachineSettingsDialog(_machineSettings);
        dialog.Owner = this;
        if (dialog.ShowDialog() == true)
        {
            _machineSettings = dialog.Settings;
            SaveSettings(); // 儲存設定
            AddLog("機台參數已更新並儲存");
        }
    }

    private void ManualControl_Click(object sender, RoutedEventArgs e)
    {
        if (_processController.IsRunning)
        {
            MessageBox.Show("流程運行中，無法開啟手動控制", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        var dialog = new JogDialog(_motionCard, _controller);
        dialog.Owner = this;
        dialog.Show();
        AddLog("開啟手動控制 (JOG)");
    }

    private void Records_Click(object sender, RoutedEventArgs e)
    {
        AddLog("開啟紀錄介面");
        MessageBox.Show("紀錄功能\n\n包含:\n• 異常履歷\n• 真空曲線", 
            "紀錄", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private async void Home_Click(object sender, RoutedEventArgs e)
    {
        if (_processController.IsRunning)
        {
            MessageBox.Show("流程運行中，無法執行原點復歸", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        var result = MessageBox.Show("確定要執行原點復歸嗎？\n\n請確認:\n• 機台無異常\n• 腔體已開啟\n• 人員已離開動作區域",
            "原點復歸確認", MessageBoxButton.YesNo, MessageBoxImage.Question);
        
        if (result == MessageBoxResult.Yes)
        {
            AddLog("開始執行原點復歸...");
            MachineStatus.Text = "原點復歸中";
            StatusText.Text = "執行原點復歸中...";
            
            var success = await _controller.HomeAllAsync();
            
            if (success)
            {
                MachineStatus.Text = "手動待機中";
                StatusText.Text = "原點復歸完成";
                AddLog("✓ 原點復歸完成");
            }
            else
            {
                MachineStatus.Text = "異常";
                StatusText.Text = "原點復歸失敗";
                AddLog("✗ 原點復歸失敗");
            }
        }
    }

    private async void Start_Click(object sender, RoutedEventArgs e)
    {
        if (_processController.CurrentState == ProcessState.Paused)
        {
            _processController.Resume();
            AddLog("流程繼續");
            return;
        }
        
        if (_processController.IsRunning)
        {
            MessageBox.Show("流程已在運行中", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        
        var result = MessageBox.Show("確定要開始自動流程嗎？\n\n請確認:\n• 已完成原點復歸\n• 工件已就位\n• 安全條件已滿足",
            "開始確認", MessageBoxButton.YesNo, MessageBoxImage.Question);
        
        if (result == MessageBoxResult.Yes)
        {
            AddLog("開始自動流程");
            StatusText.Text = "自動運行中";
            
            // 非同步執行流程
            var success = await _processController.StartAsync();
            
            StatusText.Text = success ? "流程完成" : "流程異常終止";
        }
    }

    private void Pause_Click(object sender, RoutedEventArgs e)
    {
        if (_processController.IsRunning)
        {
            if (_processController.CurrentState == ProcessState.Paused)
            {
                _processController.Resume();
                AddLog("流程繼續");
            }
            else
            {
                _processController.Pause();
                AddLog("流程暫停");
            }
        }
    }

    private async void Exit_Click(object sender, RoutedEventArgs e)
    {
        if (_processController.IsRunning)
        {
            var result = MessageBox.Show("流程正在運行，確定要停止並離開嗎？", "警告", 
                MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                await _processController.StopAsync();
            }
            else
            {
                return;
            }
        }
        
        var exitResult = MessageBox.Show("確定要離開系統嗎？", "離開", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (exitResult == MessageBoxResult.Yes)
        {
            AddLog("系統關閉");
            Close();
        }
    }

    private void AddLog(string message)
    {
        string timestamp = DateTime.Now.ToString("HH:mm:ss");
        string logEntry = $"[{timestamp}] {message}";
        
        LogToFile(logEntry); // 寫入檔案
        
        ProcessLog.Items.Insert(0, logEntry);
        if (ProcessLog.Items.Count > 100) ProcessLog.Items.RemoveAt(100);
    }

    // ===== 檔案存取 =====

    private const string DataPath = @"D:\Data\VCD";
    private const string LogPath = @"D:\Data\VCD\Logs";
    private const string RecipePath = @"D:\Data\VCD\Recipes";
    private const string SettingsPath = @"D:\Data\VCD\Settings";

    private void InitStorage()
    {
        try
        {
            Directory.CreateDirectory(DataPath);
            Directory.CreateDirectory(LogPath);
            Directory.CreateDirectory(RecipePath);
            Directory.CreateDirectory(SettingsPath);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"無法建立資料夾: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LogToFile(string entry)
    {
        try
        {
            string filename = Path.Combine(LogPath, $"{DateTime.Now:yyyyMMdd}.log");
            File.AppendAllText(filename, entry + Environment.NewLine);
        }
        catch { /* 忽略日誌寫入錯誤以免影響主流程 */ }
    }

    private void SaveSettings()
    {
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            
            // 儲存 Recipe
            string recipeFile = Path.Combine(RecipePath, "Default.json");
            string recipeJson = JsonSerializer.Serialize(_currentRecipe, options);
            File.WriteAllText(recipeFile, recipeJson);
            
            // 儲存 MachineSettings
            string machineFile = Path.Combine(SettingsPath, "Machine.json");
            string machineJson = JsonSerializer.Serialize(_machineSettings, options);
            File.WriteAllText(machineFile, machineJson);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"儲存設定失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LoadSettings()
    {
        var options = new JsonSerializerOptions 
        { 
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            PropertyNameCaseInsensitive = true
        };

        // 載入 Recipe
        string recipeFile = Path.Combine(RecipePath, "Default.json");
        try
        {
            if (File.Exists(recipeFile))
            {
                string json = File.ReadAllText(recipeFile);
                _currentRecipe = JsonSerializer.Deserialize<ProcessRecipe>(json, options) ?? new ProcessRecipe();
            }
            else
            {
                _currentRecipe = new ProcessRecipe();
                SaveSettings(); 
            }
        }
        catch (Exception ex)
        {
            AddLog($"Recipe 載入失敗: {ex.Message}，已備份並重置。");
            _currentRecipe = new ProcessRecipe();
            BackupBadFile(recipeFile);
            SaveSettings();
        }
        _processController.UpdateRecipe(_currentRecipe);
        
        // 載入 MachineSettings
        string machineFile = Path.Combine(SettingsPath, "Machine.json");
        try
        {
            if (File.Exists(machineFile))
            {
                string json = File.ReadAllText(machineFile);
                _machineSettings = JsonSerializer.Deserialize<MachineSettings>(json, options) ?? new MachineSettings();
            }
            else
            {
                _machineSettings = new MachineSettings();
                SaveSettings();
            }
        }
        catch (Exception ex)
        {
            AddLog($"機台參數載入失敗: {ex.Message}，已備份並重置。");
            _machineSettings = new MachineSettings();
            BackupBadFile(machineFile);
            SaveSettings();
        }
    }

    private void BackupBadFile(string filePath)
    {
        try 
        { 
            if (File.Exists(filePath))
            {
                string backupPath = filePath + ".bak." + DateTime.Now.ToString("yyyyMMddHHmmss");
                File.Move(filePath, backupPath); 
            }
        } 
        catch { }
    }

    protected override async void OnClosed(EventArgs e)
    {
        _updateTimer.Stop();
        if (_processController.IsRunning)
        {
            await _processController.StopAsync();
        }
        if (_motionCard != null) _motionCard.Dispose();
        base.OnClosed(e);
    }
}
