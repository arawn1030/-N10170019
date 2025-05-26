using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;



namespace NumberSearchApp;

public partial class InputPanel : ContentPage
{

    private List<string> manualRecords = new(); // 手動紀錄列表
    public InputPanel()
	{
		InitializeComponent();
        _ = LoadAllDataAsync(); // ?? 新增：初始化時讀取儲存檔案
    }

    // ?? 按下「清除輸入」按鈕：清空手動輸入欄位
    private void OnClearManualInputClicked(object sender, EventArgs e)
    {
        manualInputEntry.Text = string.Empty;
    }


    // ?? 按下「儲存手動紀錄」按鈕：把輸入欄位的資料存進 manualRecords 並寫入檔案
    private async void OnSaveManualRecordClicked(object sender, EventArgs e)
    {
        string input = manualInputEntry.Text?.Trim();
        if (!string.IsNullOrEmpty(input))
        {
            manualRecords.Add(input);
            await SaveAllDataAsync(new List<string>()); // 儲存進檔案
            await DisplayAlert("已儲存", $"已儲存牌局：{input}", "好");
            manualInputEntry.Text = string.Empty;
        }
        else
        {
            await DisplayAlert("錯誤", "請先輸入牌局內容", "好");
        }
    }

    // ?? 按下「查看手動紀錄牌局」按鈕：從 AppDataDirectory 下的 baccaratData.json 讀取 ManualHistory 並顯示，載入 AppPackage 的原始資料 + AppDataDirectory 自訂資料
    private async void OnShowManualRecordsClicked(object sender, EventArgs e)
    {
        try
        {
            var allManualRecords = new List<string>();

            // 第一步：讀取預設內建資料（Resources/Raw）
            using var packageStream = await FileSystem.OpenAppPackageFileAsync("baccaratData.json");
            using var reader = new StreamReader(packageStream);
            string defaultJson = await reader.ReadToEndAsync();
            var defaultData = JsonSerializer.Deserialize<DatingData>(defaultJson);

            if (defaultData?.ManualHistory != null)
                allManualRecords.AddRange(defaultData.ManualHistory);
            if (defaultData?.BaccaratHistory != null)
                allManualRecords.AddRange(
                    defaultData.BaccaratHistory
                    .Where(s => !string.IsNullOrWhiteSpace(s) && s.All(char.IsDigit))
                    .Distinct()
                );
            // 第二步：讀取 AppDataDirectory 的自訂資料
            string filePath = Path.Combine(FileSystem.AppDataDirectory, "baccaratData.json");
            if (File.Exists(filePath))
            {
                string customJson = await File.ReadAllTextAsync(filePath);
                var customData = JsonSerializer.Deserialize<DatingData>(customJson);

                if (customData?.ManualHistory != null)
                    allManualRecords.AddRange(customData.ManualHistory);
            }

            // 第三步：更新顯示（去除重複）
            manualRecords = allManualRecords.Distinct().ToList();

            if (manualRecords.Count == 0)
                manualRecordsLabel.Text = "手動紀錄：尚無記錄";
            else
                manualRecordsLabel.Text = "手動紀錄：\n" + string.Join("\n", manualRecords);
        }
        catch (Exception ex)
        {
            manualRecordsLabel.Text = $"讀取手動紀錄時發生錯誤：{ex.Message}";
        }
    }



    // ?? 頁面顯示時觸發：讀取 baccaratData.json 中的手動紀錄並寫入記憶
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        string filePath = Path.Combine(FileSystem.AppDataDirectory, "baccaratData.json");
        if (File.Exists(filePath))
        {
            try
            {
                var json = await File.ReadAllTextAsync(filePath);
                var data = JsonSerializer.Deserialize<DatingData>(json);
                manualRecords = data?.ManualHistory ?? new List<string>();
            }
            catch
            {
                manualRecords = new();
            }
        }
    }


    // ?? 將模擬結果與手動紀錄一併儲存至 baccaratData.json
    private async Task SaveAllDataAsync(List<string> simulatedRecords)
    {
        try
        {
            string filePath = Path.Combine(FileSystem.AppDataDirectory, "baccaratData.json");

            DatingData data;
            if (File.Exists(filePath))
            {
                var json = await File.ReadAllTextAsync(filePath);
                data = JsonSerializer.Deserialize<DatingData>(json) ?? new DatingData();
            }
            else
            {
                data = new DatingData();
            }

            if (data.BaccaratHistory == null)
                data.BaccaratHistory = new();

            if (data.ManualHistory == null)
                data.ManualHistory = new();

            foreach (var record in simulatedRecords)
            {
                if (!string.IsNullOrWhiteSpace(record))
                    data.BaccaratHistory.Add(record);
            }

            // 加入目前暫存的手動紀錄（由手動輸入區塊維護）
            foreach (var manual in manualRecords)
            {
                if (!string.IsNullOrWhiteSpace(manual) && !data.ManualHistory.Contains(manual))
                    data.ManualHistory.Add(manual);
            }

            var newJson = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(filePath, newJson);
        }
        catch (Exception ex)
        {
            await DisplayAlert("儲存失敗", $"模擬紀錄儲存失敗：{ex.Message}", "好");
        }
    }

    //載入所有資料並更新 manualRecords 清單
    private async Task LoadAllDataAsync()
    {
        try
        {
            string filePath = Path.Combine(FileSystem.AppDataDirectory, "baccaratData.json");

            if (File.Exists(filePath))
            {
                var json = await File.ReadAllTextAsync(filePath);
                var data = JsonSerializer.Deserialize<DatingData>(json);

                if (data != null && data.ManualHistory != null)
                {
                    manualRecords.Clear(); // 清掉舊的
                    manualRecords.AddRange(data.ManualHistory); // ? 正確讀入手動紀錄
                }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("讀取錯誤", $"載入資料失敗：{ex.Message}", "好");
        }
    }

}