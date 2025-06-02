using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.Json;
using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel; // for Platform.CurrentActivity


namespace NumberSearchApp
{
    public partial class MainPage : ContentPage
    {
        private List<string> baccaratHistory = new();   // 儲存所有百家樂紀錄（原始+模擬）
        private List<string> manualHistory = new();     // 儲存所有手動輸入紀錄


        public MainPage()
        {

            InitializeComponent();
            LoadInitialData();// 載入資料來源
        }


        // 初始載入資料
        private async void LoadInitialData()
        {
            try
            {
                // 嘗試清理資料中無效的紀錄（例如非純數字或破損 JSON）
                await CleanInvalidBaccaratDataAsync();

                // 載入 Resources/Raw/baccaratData.json 預設資料
                using var stream = await FileSystem.OpenAppPackageFileAsync("baccaratData.json");
                using var reader = new StreamReader(stream);
                string defaultJson = await reader.ReadToEndAsync();
                var defaultData = JsonSerializer.Deserialize<BaccaratData>(defaultJson);

                var allData = new List<string>();

                // 加入預設資料中的 BaccaratHistory
                if (defaultData?.BaccaratHistory != null)
                    allData.AddRange(defaultData.BaccaratHistory);

                // 從 AppData 資料夾讀取額外的模擬與手動紀錄
                string customFilePath = Path.Combine(FileSystem.AppDataDirectory, "baccaratData.json");
                if (File.Exists(customFilePath))
                {
                    string customJson = await File.ReadAllTextAsync(customFilePath);
                    var customData = JsonSerializer.Deserialize<BaccaratData>(customJson);

                    if (customData?.BaccaratHistory != null)
                        allData.AddRange(customData.BaccaratHistory);

                    // 載入手動輸入的紀錄，過濾空白與非數字
                    if (customData?.ManualHistory != null)
                        manualHistory = customData.ManualHistory
                            .Where(s => !string.IsNullOrWhiteSpace(s) && s.All(char.IsDigit))
                            .Distinct()
                            .ToList();
                }

                // 去除重複
                baccaratHistory = allData.Distinct().ToList();

                TotalMatchesLabel.Text = $"資料載入成功，共 {baccaratHistory.Count} 筆";
            }
            catch (Exception ex)
            {
                TotalMatchesLabel.Text = $"資料載入失敗：{ex.Message}";
            }
        }


        // 嘗試修正 AppData 中 baccaratData.json 檔案格式問題或內容異常。
        private async Task CleanInvalidBaccaratDataAsync()
        {
            try
            {
                string filePath = Path.Combine(FileSystem.AppDataDirectory, "baccaratData.json");

                if (!File.Exists(filePath))
                    return;

                string json = await File.ReadAllTextAsync(filePath);

                var options = new JsonSerializerOptions
                {
                    AllowTrailingCommas = true,
                    ReadCommentHandling = JsonCommentHandling.Skip
                };

                BaccaratData? data = null;

                try
                {
                    // 嘗試正常反序列化整份檔案
                    data = JsonSerializer.Deserialize<BaccaratData>(json, options);
                }
                catch
                {
                    try
                    {
                        // 嘗試用 JsonDocument 分析（如果格式不正確仍會失敗）
                        using var doc = JsonDocument.Parse(json);
                        var root = doc.RootElement;

                        if (root.TryGetProperty("BaccaratHistory", out var array) && array.ValueKind == JsonValueKind.Array)
                        {
                            var cleanedList = new List<string>();
                            foreach (var item in array.EnumerateArray())
                            {
                                if (item.ValueKind == JsonValueKind.String)
                                {
                                    string? entry = item.GetString();
                                    if (!string.IsNullOrWhiteSpace(entry) && entry.All(char.IsDigit))
                                        cleanedList.Add(entry);
                                }
                            }
                            data = new BaccaratData { BaccaratHistory = cleanedList };
                        }
                    }
                    catch
                    {
                        // 若完全無法修復，直接備份並重建檔案
                        string backupPath = filePath + ".bak";
                        File.Copy(filePath, backupPath, true);
                        data = new BaccaratData(); // 空白資料
                    }
                }

                if (data != null)
                {
                    // 最終資料清洗
                    data.BaccaratHistory = data.BaccaratHistory
                        .Where(s => !string.IsNullOrWhiteSpace(s) && s.All(char.IsDigit))
                        .Distinct()
                        .ToList();

                    string fixedJson = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                    await File.WriteAllTextAsync(filePath, fixedJson);
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("錯誤", $"清理資料時失敗：{ex.Message}", "好");
            }
        }


        // 搜尋百家樂資料中是否含有會員編號，統計出現次數與後方 12 碼。
        private async void OnSearchClicked(object sender, EventArgs e)
        {
            string searchValue = SearchEntry.Text?.Trim();
            if (string.IsNullOrEmpty(searchValue))
            {
                await DisplayAlert("提示", "請輸入編號", "好");
                return;
            }

            TotalMatchesLabel.Text = "搜尋中...";

            if (baccaratHistory.Count == 0)
            {
                TotalMatchesLabel.Text = "尚未載入資料，無法搜尋。";
                return;
            }

            var afterSixDigits = new Dictionary<string, int>();
            int totalMatches = 0;

            foreach (var number in baccaratHistory)
            {
                int index = 0;
                while ((index = number.IndexOf(searchValue, index)) != -1)
                {
                    int afterIndex = index + searchValue.Length;
                    if (afterIndex + 12 <= number.Length)
                    {
                        string afterDigits = number.Substring(afterIndex, 12);
                        if (afterSixDigits.ContainsKey(afterDigits))
                            afterSixDigits[afterDigits]++;
                        else
                            afterSixDigits[afterDigits] = 1;
                    }
                    totalMatches++;
                    index += searchValue.Length;
                }
            }

            TotalMatchesLabel.Text = $"共找到 {totalMatches} 次記錄";

            var resultList = afterSixDigits
                .OrderByDescending(x => x.Value)
                .Select(x => new SearchResult
                {
                    RawDigits = x.Key,
                    CountNumber = x.Value
                })
                .ToList();

            ResultsCollectionView.ItemsSource = resultList;

            // 收起鍵盤
            SearchEntry.Unfocus();





        }

        // 清除搜尋欄位與結果
        private void OnClearClicked(object sender, EventArgs e)
        {
            SearchEntry.Text = string.Empty;
            ResultsCollectionView.ItemsSource = null;
            TotalMatchesLabel.Text = "已清除查詢結果";
            SearchEntry.Focus(); // 讓游標回到輸入欄
        }

        // 導航到模擬頁面
        private async void OnGoToSimulationClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new SimulationPage());
        }

        // 導航到手動輸入頁面
        private async void OnNavigateToInputPanelClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new InputPanel());
        }

        // 導航到獲利分析頁面
        private async void OnNavigateToProfitPageClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new ProfitPage());
        }

        // 搜尋手動輸入的資料（manualHistory）
        private async void OnSearchManualClicked(object sender, EventArgs e)
        {
            await SearchDataAsync(manualHistory, "手動紀錄");
        }

        // 共用搜尋邏輯：搜尋指定資料集，統計出現後續 12 碼
        private async Task SearchDataAsync(List<string> sourceData, string label)
        {
            string searchValue = SearchEntry.Text?.Trim();
            if (string.IsNullOrEmpty(searchValue))
            {
                await DisplayAlert("提示", "請輸入會員編號", "好");
                return;
            }

            TotalMatchesLabel.Text = $"搜尋{label}中...";

            var afterSixDigits = new Dictionary<string, int>();
            int totalMatches = 0;

            foreach (var number in sourceData)
            {
                int index = 0;
                while ((index = number.IndexOf(searchValue, index)) != -1)
                {
                    int afterIndex = index + searchValue.Length;
                    if (afterIndex + 12 <= number.Length)
                    {
                        string afterDigits = number.Substring(afterIndex, 12);
                        if (afterSixDigits.ContainsKey(afterDigits))
                            afterSixDigits[afterDigits]++;
                        else
                            afterSixDigits[afterDigits] = 1;
                    }
                    totalMatches++;
                    index += searchValue.Length;
                }
            }

            TotalMatchesLabel.Text = $"【{label}】共找到 {totalMatches} 次聯絡記錄";

            var resultList = afterSixDigits
                .OrderByDescending(x => x.Value)
                .Select(x => new SearchResult
                {
                    RawDigits = x.Key,
                    CountNumber = x.Value
                })
                .ToList();

            ResultsCollectionView.ItemsSource = resultList;
            SearchEntry.Focus();
            SearchEntry.CursorPosition = 0;
        }

        // 登出並回到登入頁
        private void OnLogoutClicked(object sender, EventArgs e)
        {
            // 導回登入頁
            Application.Current.MainPage = new LoginPage();
        }

       


    }

    // 用於 JSON 資料結構對應
    public class BaccaratData
    {
        public List<string> BaccaratHistory { get; set; } = new();
        public List<string> ManualHistory { get; set; } = new();
    }

    // 用於顯示搜尋結果的資料模型
    public class SearchResult
    {
        public string RawDigits { get; set; }  // 原始6碼
        public int CountNumber { get; set; }   // 次數

        public string DisplayText
        {
            get
            {
                if (string.IsNullOrEmpty(RawDigits) || RawDigits.Length < 6)
                    return "";

                string first = RawDigits[0].ToString();
                string rest = RawDigits.Substring(1);
                return $"{first}{rest}     {CountNumber}";
            }
        }
    }


}