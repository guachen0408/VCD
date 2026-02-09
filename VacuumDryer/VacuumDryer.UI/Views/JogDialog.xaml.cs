using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using VacuumDryer.Core.Motion;
using VacuumDryer.Hardware;

namespace VacuumDryer.UI.Views;

public partial class JogDialog : Window
{
    private readonly IMotionCard _motionCard;
    private readonly DualZController _controller;
    private readonly DispatcherTimer _updateTimer;

    public JogDialog(IMotionCard motionCard, DualZController controller)
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
        ValvePosition.Text = _controller.ValvePosition.ToString("F3");
        Z1Position.Text = _controller.PositionZ1.ToString("F3");
        SyncError.Text = $"{_controller.SyncError:F3} mm";
    }

    // ===== 分頁切換 =====
    private void TabValve_Click(object sender, MouseButtonEventArgs e)
    {
        TabValve.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));
        TabChamber.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0"));
        ((TextBlock)TabValve.Child).Foreground = Brushes.White;
        ((TextBlock)TabChamber.Child).Foreground = Brushes.Black;
        ValvePanel.Visibility = Visibility.Visible;
        ChamberPanel.Visibility = Visibility.Collapsed;
    }

    private void TabChamber_Click(object sender, MouseButtonEventArgs e)
    {
        TabValve.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0"));
        TabChamber.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));
        ((TextBlock)TabValve.Child).Foreground = Brushes.Black;
        ((TextBlock)TabChamber.Child).Foreground = Brushes.White;
        ValvePanel.Visibility = Visibility.Collapsed;
        ChamberPanel.Visibility = Visibility.Visible;
    }

    // ===== JOG 控制 =====
    private void Jog_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is Button button && button.Tag is string direction)
        {
            if (!double.TryParse(SpeedInput.Text, out double velocity))
                velocity = 10;

            switch (direction)
            {
                case "V+":
                    _ = _controller.ValveAxis.JogAsync(velocity);
                    break;
                case "V-":
                    _ = _controller.ValveAxis.JogAsync(-velocity);
                    break;
                case "Z+":
                    _ = _controller.Z1Axis.JogAsync(velocity);
                    _ = _controller.Z2Axis.JogAsync(velocity);
                    break;
                case "Z-":
                    _ = _controller.Z1Axis.JogAsync(-velocity);
                    _ = _controller.Z2Axis.JogAsync(-velocity);
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
                case "V+":
                case "V-":
                    await _controller.ValveAxis.StopAsync();
                    break;
                case "Z+":
                case "Z-":
                    await _controller.Z1Axis.StopAsync();
                    await _controller.Z2Axis.StopAsync();
                    break;
            }
        }
    }

    private async void Home_Click(object sender, RoutedEventArgs e)
    {
        await _controller.HomeAllAsync();
    }

    private async void AlmReset_Click(object sender, RoutedEventArgs e)
    {
        await _controller.Z1Axis.ClearAlarmAsync();
        await _controller.Z2Axis.ClearAlarmAsync();
        await _controller.ValveAxis.ClearAlarmAsync();
        MessageBox.Show("異常已清除", "Alm Reset", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private async void Stop_Click(object sender, RoutedEventArgs e)
    {
        await _controller.EmergencyStopAsync();
    }

    private async void ValveOff_Click(object sender, RoutedEventArgs e)
    {
        await _controller.SetValveAsync(0, 30);
    }

    protected override void OnClosed(EventArgs e)
    {
        _updateTimer.Stop();
        base.OnClosed(e);
    }
}
