using System.Text.Json;

namespace VacuumDryer.Core.Recipe;

/// <summary>
/// 配方管理器 - 管理配方的載入、儲存、匯出、版本控制
/// </summary>
public class RecipeManager<TRecipe> where TRecipe : class, new()
{
    private readonly string _recipeDirectory;
    private readonly JsonSerializerOptions _jsonOptions;
    
    private TRecipe _currentRecipe = new();
    private string _currentFileName = "default";
    
    /// <summary>目前配方</summary>
    public TRecipe Current => _currentRecipe;
    
    /// <summary>目前檔名</summary>
    public string CurrentFileName => _currentFileName;
    
    /// <summary>配方變更事件</summary>
    public event Action<TRecipe>? OnRecipeChanged;
    
    /// <summary>配方儲存事件</summary>
    public event Action<string>? OnRecipeSaved;
    
    public RecipeManager(string recipeDirectory = "Recipes")
    {
        _recipeDirectory = recipeDirectory;
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        
        if (!Directory.Exists(_recipeDirectory))
            Directory.CreateDirectory(_recipeDirectory);
    }
    
    /// <summary>
    /// 載入配方
    /// </summary>
    public TRecipe Load(string fileName)
    {
        var filePath = GetFilePath(fileName);
        
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"配方檔案不存在: {fileName}");
        
        var json = File.ReadAllText(filePath);
        _currentRecipe = JsonSerializer.Deserialize<TRecipe>(json, _jsonOptions) ?? new();
        _currentFileName = fileName;
        
        OnRecipeChanged?.Invoke(_currentRecipe);
        return _currentRecipe;
    }
    
    /// <summary>
    /// 儲存配方
    /// </summary>
    public void Save(string? fileName = null)
    {
        fileName ??= _currentFileName;
        var filePath = GetFilePath(fileName);
        
        // 備份現有檔案
        if (File.Exists(filePath))
        {
            var backupPath = filePath + $".bak.{DateTime.Now:yyyyMMdd_HHmmss}";
            File.Copy(filePath, backupPath, true);
        }
        
        var json = JsonSerializer.Serialize(_currentRecipe, _jsonOptions);
        File.WriteAllText(filePath, json);
        
        _currentFileName = fileName;
        OnRecipeSaved?.Invoke(fileName);
    }
    
    /// <summary>
    /// 另存新配方
    /// </summary>
    public void SaveAs(string fileName)
    {
        Save(fileName);
    }
    
    /// <summary>
    /// 更新目前配方
    /// </summary>
    public void Update(TRecipe recipe)
    {
        _currentRecipe = recipe;
        OnRecipeChanged?.Invoke(_currentRecipe);
    }
    
    /// <summary>
    /// 刪除配方
    /// </summary>
    public bool Delete(string fileName)
    {
        var filePath = GetFilePath(fileName);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            return true;
        }
        return false;
    }
    
    /// <summary>
    /// 取得所有配方名稱
    /// </summary>
    public IReadOnlyList<string> GetAllNames()
    {
        return Directory.GetFiles(_recipeDirectory, "*.json")
            .Select(Path.GetFileNameWithoutExtension)
            .Where(n => n != null)
            .Select(n => n!)
            .OrderBy(n => n)
            .ToList()
            .AsReadOnly();
    }
    
    /// <summary>
    /// 配方是否存在
    /// </summary>
    public bool Exists(string fileName)
    {
        return File.Exists(GetFilePath(fileName));
    }
    
    /// <summary>
    /// 載入或建立預設配方
    /// </summary>
    public TRecipe LoadOrCreateDefault(string fileName = "default")
    {
        if (Exists(fileName))
        {
            return Load(fileName);
        }
        
        _currentRecipe = new();
        _currentFileName = fileName;
        Save(fileName);
        return _currentRecipe;
    }
    
    /// <summary>
    /// 複製配方
    /// </summary>
    public void Copy(string sourceFileName, string targetFileName)
    {
        var sourcePath = GetFilePath(sourceFileName);
        var targetPath = GetFilePath(targetFileName);
        
        if (!File.Exists(sourcePath))
            throw new FileNotFoundException($"來源配方不存在: {sourceFileName}");
        
        File.Copy(sourcePath, targetPath, true);
    }
    
    private string GetFilePath(string fileName)
    {
        if (!fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            fileName += ".json";
        
        return Path.Combine(_recipeDirectory, fileName);
    }
}
