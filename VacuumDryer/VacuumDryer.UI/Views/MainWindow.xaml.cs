using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using VacuumDryer.Core.Motion;
using VacuumDryer.Core.Process;
using VacuumDryer.Hardware;
using VacuumDryer.Hardware.Simulation;

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
        
        // 使用模擬控制卡 (實機時換成 DeltaPciL221MotionCard)
        _motionCard = new SimulatedMotionCard3Axis();
        _motionCard.InitializeAsync().Wait();
        
        _controller = new DualZController(_motionCard);
        _processController = new VacuumProcessController(_controller, _motionCard.GetDigitalIO());
        
        // 訂閱流程事件
        _processController.OnStateChanged += ProcessController_OnStateChanged;
        _processController.OnProcessLog += ProcessController_OnProcessLog;
        _processController.OnAlarm += ProcessController_OnAlarm;
        
        _updateTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
        _updateTimer.Tick += UpdateTimer_Tick;
        _updateTimer.Start();
        
        AddLog("系統初始化完成");
        AddLog($"控制卡: {_motionCard.Name}");
    }

    private void UpdateTimer_Tick(object? sender, EventArgs e)
    {
        // 軸位置更新
        ValveAngle.Text = _controller.ValvePosition.ToString("F3");
        ChamberPosition.Text = _controller.PositionZ1.ToString("F3");
        
        // 壓力更新
        CurrentPressure.Text = _processController.CurrentPressure.ToString("F2");
        
        // 時間更新
        TimeText.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }

    // ===== 流程控制器事件 =====
    
    private void ProcessController_OnStateChanged(ProcessState state, string message)
    {
        Dispatcher.Invoke(() =>
        {
            // 更新機台狀態顯示
            MachineStatus.Text = GetStateDisplayName(state);
            
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
    private void TabMain_Click(object sender, MouseButtonEventArgs e)
    {
        TabMain.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));
        TabChart.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0"));
        TabAlarm.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0"));
        ((TextBlock)TabMain.Child).Foreground = Brushes.White;
        ((TextBlock)TabChart.Child).Foreground = Brushes.Black;
        ((TextBlock)TabAlarm.Child).Foreground = Brushes.Black;
    }

    private void TabChart_Click(object sender, MouseButtonEventArgs e)
    {
        TabMain.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0"));
        TabChart.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));
        TabAlarm.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0"));
        ((TextBlock)TabMain.Child).Foreground = Brushes.Black;
        ((TextBlock)TabChart.Child).Foreground = Brushes.White;
        ((TextBlock)TabAlarm.Child).Foreground = Brushes.Black;
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
        AddLog("切換至異常頁面");
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
        AddLog("開啟設定介面");
        MessageBox.Show("設定功能\n\n包含:\n• 機台參數設定\n• 材料/塗佈參數", 
            "設定", MessageBoxButton.OK, MessageBoxImage.Information);
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
        ProcessLog.Items.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {message}");
        if (ProcessLog.Items.Count > 100) ProcessLog.Items.RemoveAt(100);
    }

    protected override async void OnClosed(EventArgs e)
    {
        _updateTimer.Stop();
        if (_processController.IsRunning)
        {
            await _processController.StopAsync();
        }
        _motionCard.Dispose();
        base.OnClosed(e);
    }
}
