using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using VacuumDryer.Core.Motion;
using VacuumDryer.Hardware;

namespace VacuumDryer.UI.Views;

public partial class ManualControlDialog : Window
{
    private readonly IMotionCard _motionCard;
    private readonly DualZController _controller;
    private readonly DispatcherTimer _updateTimer;

    public ManualControlDialog(IMotionCard motionCard, DualZController controller)
    {
        InitializeComponent();
        
        _motionCard = motionCard;
        _controller = controller;
        
        _updateTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
        _updateTimer.Tick += UpdateTimer_Tick;
        _updateTimer.Start();
    }

    private void UpdateTimer_Tick(object? sender, EventArgs e)
    {
        Z1Position.Text = _controller.PositionZ1.ToString("F3");
        Z2Position.Text = _controller.PositionZ2.ToString("F3");
        ValvePosition.Text = _controller.ValvePosition.ToString("F1");
        
        var syncError = _controller.SyncError;
        SyncError.Text = syncError.ToString("F3");
        SyncError.Foreground = syncError > 0.1 
            ? new SolidColorBrush(Colors.Red) 
            : new SolidColorBrush(Color.FromRgb(0, 255, 136));
    }

    private void Jog_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is Button button && button.Tag is string direction)
        {
            var velocity = 50.0;
            
            switch (direction)
            {
                case "Z+":
                    _ = _controller.Z1Axis.JogAsync(velocity);
                    _ = _controller.Z2Axis.JogAsync(velocity);
                    StatusText.Text = "Z 軸上升中...";
                    break;
                case "Z-":
                    _ = _controller.Z1Axis.JogAsync(-velocity);
                    _ = _controller.Z2Axis.JogAsync(-velocity);
                    StatusText.Text = "Z 軸下降中...";
                    break;
                case "V+":
                    _ = _controller.ValveAxis.JogAsync(velocity);
                    StatusText.Text = "蝶閥開啟中...";
                    break;
                case "V-":
                    _ = _controller.ValveAxis.JogAsync(-velocity);
                    StatusText.Text = "蝶閥關閉中...";
                    break;
            }
        }
    }

    private async void Jog_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is Button button && button.Tag is string direction)
        {
            switch (direction)
            {
                case "Z+":
                case "Z-":
                    await _controller.Z1Axis.StopAsync();
                    await _controller.Z2Axis.StopAsync();
                    break;
                case "V+":
                case "V-":
                    await _controller.ValveAxis.StopAsync();
                    break;
            }
            StatusText.Text = "就緒";
        }
    }

    private async void ValvePreset_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string percentStr)
        {
            if (double.TryParse(percentStr, out double percent))
            {
                StatusText.Text = $"蝶閥移動至 {percent}%...";
                await _controller.SetValveAsync(percent, 30);
                StatusText.Text = "就緒";
            }
        }
    }

    private async void Home_Click(object sender, RoutedEventArgs e)
    {
        StatusText.Text = "正在回原點...";
        var success = await _controller.HomeAllAsync();
        StatusText.Text = success ? "回原點完成" : "回原點失敗";
    }

    private async void EnableServo_Click(object sender, RoutedEventArgs e)
    {
        StatusText.Text = "正在啟用伺服...";
        var success = await _controller.EnableAllAsync();
        StatusText.Text = success ? "伺服已啟用" : "啟用失敗";
    }

    private async void DisableServo_Click(object sender, RoutedEventArgs e)
    {
        StatusText.Text = "正在停用伺服...";
        var success = await _controller.DisableAllAsync();
        StatusText.Text = success ? "伺服已停用" : "停用失敗";
    }

    protected override void OnClosed(EventArgs e)
    {
        _updateTimer.Stop();
        base.OnClosed(e);
    }
}
