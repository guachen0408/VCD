using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace VacuumDryer.Core.Auth;

/// <summary>
/// 使用者角色 (權限等級由低到高)
/// </summary>
public enum UserRole
{
    Viewer = 0,     // 檢視者: 只能看監控畫面
    Operator = 1,   // 操作員: 可啟動/停止流程、手動操作
    Engineer = 2,   // 工程師: 可修改配方、機械設定、JOG
    Admin = 3       // 管理員: 可管理帳號、存取所有功能
}

/// <summary>
/// 使用者帳號
/// </summary>
public class UserAccount
{
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Operator;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? LastLoginAt { get; set; }
    public bool IsEnabled { get; set; } = true;
}

/// <summary>
/// 權限管理器 - 管理使用者帳號、登入、權限檢查
/// 適用於自動化機台的操作員/工程師/管理員三級權限
/// </summary>
public class AuthManager
{
    private readonly string _accountsFilePath;
    private List<UserAccount> _accounts = new();
    private UserAccount? _currentUser;
    
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter() }
    };
    
    /// <summary>目前登入的使用者</summary>
    public UserAccount? CurrentUser => _currentUser;
    
    /// <summary>目前角色</summary>
    public UserRole CurrentRole => _currentUser?.Role ?? UserRole.Viewer;
    
    /// <summary>是否已登入</summary>
    public bool IsLoggedIn => _currentUser != null;
    
    // ===== 事件 =====
    
    /// <summary>登入事件</summary>
    public event Action<UserAccount>? OnLogin;
    
    /// <summary>登出事件</summary>
    public event Action? OnLogout;
    
    /// <summary>權限不足事件 (操作名稱, 所需角色)</summary>
    public event Action<string, UserRole>? OnAccessDenied;
    
    public AuthManager(string accountsFilePath = "Config/accounts.json")
    {
        _accountsFilePath = accountsFilePath;
        LoadAccounts();
    }
    
    // ===== 登入/登出 =====
    
    /// <summary>
    /// 登入
    /// </summary>
    public bool Login(string username, string password)
    {
        var account = _accounts.FirstOrDefault(a => 
            a.Username.Equals(username, StringComparison.OrdinalIgnoreCase) && a.IsEnabled);
        
        if (account == null) return false;
        
        var hash = HashPassword(password);
        if (account.PasswordHash != hash) return false;
        
        _currentUser = account;
        account.LastLoginAt = DateTime.Now;
        SaveAccounts();
        
        OnLogin?.Invoke(account);
        return true;
    }
    
    /// <summary>
    /// 登出
    /// </summary>
    public void Logout()
    {
        _currentUser = null;
        OnLogout?.Invoke();
    }
    
    // ===== 權限檢查 =====
    
    /// <summary>
    /// 檢查是否有權限執行操作
    /// </summary>
    public bool HasPermission(UserRole requiredRole)
    {
        return CurrentRole >= requiredRole;
    }
    
    /// <summary>
    /// 檢查權限並觸發事件 (用於 UI 按鈕)
    /// </summary>
    public bool CheckPermission(UserRole requiredRole, string operationName = "")
    {
        if (HasPermission(requiredRole)) return true;
        
        OnAccessDenied?.Invoke(operationName, requiredRole);
        return false;
    }
    
    // ===== 帳號管理 (需 Admin 權限) =====
    
    /// <summary>
    /// 新增帳號
    /// </summary>
    public bool AddUser(string username, string password, UserRole role)
    {
        if (!HasPermission(UserRole.Admin)) return false;
        if (_accounts.Any(a => a.Username.Equals(username, StringComparison.OrdinalIgnoreCase)))
            return false;
        
        _accounts.Add(new UserAccount
        {
            Username = username,
            PasswordHash = HashPassword(password),
            Role = role,
            CreatedAt = DateTime.Now
        });
        SaveAccounts();
        return true;
    }
    
    /// <summary>
    /// 修改密碼
    /// </summary>
    public bool ChangePassword(string username, string newPassword)
    {
        // 自己改密碼 或 Admin 改別人密碼
        var isSelf = _currentUser?.Username.Equals(username, StringComparison.OrdinalIgnoreCase) ?? false;
        if (!isSelf && !HasPermission(UserRole.Admin)) return false;
        
        var account = _accounts.FirstOrDefault(a => 
            a.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
        if (account == null) return false;
        
        account.PasswordHash = HashPassword(newPassword);
        SaveAccounts();
        return true;
    }
    
    /// <summary>
    /// 修改角色
    /// </summary>
    public bool ChangeRole(string username, UserRole newRole)
    {
        if (!HasPermission(UserRole.Admin)) return false;
        
        var account = _accounts.FirstOrDefault(a => 
            a.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
        if (account == null) return false;
        
        account.Role = newRole;
        SaveAccounts();
        return true;
    }
    
    /// <summary>
    /// 停用帳號
    /// </summary>
    public bool DisableUser(string username)
    {
        if (!HasPermission(UserRole.Admin)) return false;
        
        var account = _accounts.FirstOrDefault(a => 
            a.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
        if (account == null) return false;
        
        account.IsEnabled = false;
        SaveAccounts();
        return true;
    }
    
    /// <summary>
    /// 取得所有帳號 (需 Admin)
    /// </summary>
    public IReadOnlyList<UserAccount> GetAllUsers()
    {
        if (!HasPermission(UserRole.Admin)) return new List<UserAccount>().AsReadOnly();
        return _accounts.AsReadOnly();
    }
    
    // ===== 內部方法 =====
    
    private void LoadAccounts()
    {
        try
        {
            if (File.Exists(_accountsFilePath))
            {
                var json = File.ReadAllText(_accountsFilePath);
                _accounts = JsonSerializer.Deserialize<List<UserAccount>>(json, _jsonOptions) ?? new();
            }
        }
        catch
        {
            _accounts = new();
        }
        
        // 確保有預設管理員帳號
        if (!_accounts.Any(a => a.Role == UserRole.Admin))
        {
            _accounts.Add(new UserAccount
            {
                Username = "admin",
                PasswordHash = HashPassword("admin"),
                Role = UserRole.Admin,
                CreatedAt = DateTime.Now
            });
            SaveAccounts();
        }
    }
    
    private void SaveAccounts()
    {
        var dir = Path.GetDirectoryName(_accountsFilePath);
        if (dir != null && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        
        var json = JsonSerializer.Serialize(_accounts, _jsonOptions);
        File.WriteAllText(_accountsFilePath, json);
    }
    
    private static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes).ToLower();
    }
}
