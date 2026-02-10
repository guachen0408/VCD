using System.Windows;
using System.Windows.Input;
using VacuumDryer.Core.Auth;

namespace VacuumDryer.UI.Views;

public partial class LoginDialog : Window
{
    private readonly AuthManager _authManager;
    
    /// <summary>登入是否成功</summary>
    public bool LoginSuccess { get; private set; }
    
    public LoginDialog(AuthManager authManager)
    {
        InitializeComponent();
        _authManager = authManager;
        
        // 顯示目前登入狀態
        UpdateCurrentUserDisplay();
        
        // 聚焦帳號欄位
        Loaded += (_, _) => UsernameBox.Focus();
    }
    
    private void Login_Click(object sender, RoutedEventArgs e)
    {
        DoLogin();
    }
    
    private void Logout_Click(object sender, RoutedEventArgs e)
    {
        _authManager.Logout();
        LoginSuccess = false;
        UpdateCurrentUserDisplay();
        ErrorText.Visibility = Visibility.Collapsed;
        UsernameBox.Text = "";
        PasswordBox.Password = "";
        UsernameBox.Focus();
    }
    
    private void Close_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = LoginSuccess;
        Close();
    }
    
    private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            DoLogin();
        }
    }
    
    private void DoLogin()
    {
        var username = UsernameBox.Text.Trim();
        var password = PasswordBox.Password;
        
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            ShowError("請輸入帳號和密碼");
            return;
        }
        
        if (_authManager.Login(username, password))
        {
            LoginSuccess = true;
            UpdateCurrentUserDisplay();
            ErrorText.Visibility = Visibility.Collapsed;
            
            DialogResult = true;
            Close();
        }
        else
        {
            ShowError("帳號或密碼錯誤");
            PasswordBox.Password = "";
            PasswordBox.Focus();
        }
    }
    
    private void ShowError(string message)
    {
        ErrorText.Text = $"❌ {message}";
        ErrorText.Visibility = Visibility.Visible;
    }
    
    private void UpdateCurrentUserDisplay()
    {
        if (_authManager.IsLoggedIn)
        {
            var user = _authManager.CurrentUser!;
            CurrentUserText.Text = $"目前：{user.Username} ({user.Role})";
        }
        else
        {
            CurrentUserText.Text = "目前：未登入";
        }
    }
}
