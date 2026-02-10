using System.Windows;
using VacuumDryer.Core.Process;
using System.IO;
using System.Text.Json;
using System.Linq; // Added for LINQ operations like Select, ToList
using System; // Added for Exception handling
using System.Collections.Generic; // Added for List<T>

namespace VacuumDryer.UI.Views;

public partial class RecipeSettingsDialog : Window
{
    public ProcessRecipe Recipe { get; private set; }
    private const string RecipePath = @"D:\Data\VCD\Recipes";

    public RecipeSettingsDialog(ProcessRecipe currentRecipe)
    {
        InitializeComponent();
        
        // Deep copy to disconnect from main window until saved
        var options = new JsonSerializerOptions { IncludeFields = true };
        string json = JsonSerializer.Serialize(currentRecipe, options);
        Recipe = JsonSerializer.Deserialize<ProcessRecipe>(json, options) ?? new ProcessRecipe();
        
        DataContext = Recipe;
        
        LoadRecipeFileList();
        
        // Try to select the default if exists, or just leave empty
        RecipeFileComboBox.Text = "Default";
    }

    private void LoadRecipeFileList()
    {
        try
        {
            if (Directory.Exists(RecipePath))
            {
                var files = Directory.GetFiles(RecipePath, "*.json");
                // 只顯示檔名不含副檔名
                RecipeFileComboBox.ItemsSource = files.Select(f => Path.GetFileNameWithoutExtension(f)).ToList();
            }
        }
        catch { }
    }

    private void LoadRecipe_Click(object sender, RoutedEventArgs e)
    {
        string filename = RecipeFileComboBox.Text;
        if (string.IsNullOrWhiteSpace(filename)) return;
        
        // 自動補上 .json
        if (!filename.EndsWith(".json")) filename += ".json";
        
        string fullPath = Path.Combine(RecipePath, filename);
        if (File.Exists(fullPath))
        {
            try
            {
                string json = File.ReadAllText(fullPath);
                var loadedRecipe = JsonSerializer.Deserialize<ProcessRecipe>(json);
                if (loadedRecipe != null)
                {
                    Recipe = loadedRecipe;
                    DataContext = Recipe; // Re-bind UI
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

    private void SaveAsRecipe_Click(object sender, RoutedEventArgs e)
    {
        string filename = RecipeFileComboBox.Text;
        if (string.IsNullOrWhiteSpace(filename))
        {
            MessageBox.Show("請輸入檔案名稱", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!filename.EndsWith(".json")) filename += ".json";
        
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(Recipe, options);
            File.WriteAllText(Path.Combine(RecipePath, filename), json);
            LoadRecipeFileList(); // Refresh list
            MessageBox.Show("儲存成功", "資訊", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"儲存失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        // Data is bound TwoWay, so Recipe is already updated.
        // We can just close with true.
        // Optional: Trigger validation if needed.
        
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
