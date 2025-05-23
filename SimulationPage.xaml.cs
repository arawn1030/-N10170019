using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace NumberSearchApp
{
    public partial class SimulationPage : ContentPage
    {
        private List<string> manualRecords = new(); // 手動紀錄列表

        private readonly Random random = new();

        public SimulationPage()
        {
            InitializeComponent();
            _ = LoadAllDataAsync(); // 🔹 新增：初始化時讀取儲存檔案
        }


        // 🔘 按下「模擬500局」按鈕後執行：產生模擬牌局、顯示結果、儲存到檔案
        private async void OnSimulateClicked(object sender, EventArgs e)
        {
            List<string> simulatedRecords = new();
            List<string> allDetails = new();

            //開牌規則在這裡
            for (int i = 0; i < 500; i++)
            {
                List<(string winner, string detail)> roundResults = new();
                List<string> fullDeck = GenerateShuffledDeck();

                int cutCardFromEnd = random.Next(90, 111);
                int cutCardIndex = fullDeck.Count - cutCardFromEnd;

                string burnCard = fullDeck[0];
                int burnValue = GetCardPoint(burnCard);
                if (burnValue == 0) burnValue = 10;
                fullDeck.RemoveAt(0);
                List<string> burnedCards = new();
                for (int b = 0; b < burnValue && b < fullDeck.Count; b++)
                {
                    burnedCards.Add(fullDeck[0]);
                    fullDeck.RemoveAt(0);
                }

                int usedCards = 0;
                while (usedCards < cutCardIndex - 4)
                {
                    if (fullDeck.Count < 4) break;

                    string p1 = fullDeck[0], p2 = fullDeck[1];
                    string b1 = fullDeck[2], b2 = fullDeck[3];
                    fullDeck.RemoveRange(0, 4);
                    usedCards += 4;

                    int playerTotal = (GetCardPoint(p1) + GetCardPoint(p2)) % 10;
                    int bankerTotal = (GetCardPoint(b1) + GetCardPoint(b2)) % 10;

                    string detail = $"他:{p1},{p2}({playerTotal}) vs 你:{b1},{b2}({bankerTotal})";

                    string p3 = null, b3 = null;

                    if (playerTotal < 8 && bankerTotal < 8)
                    {
                        if (playerTotal <= 5 && fullDeck.Count > 0)
                        {
                            p3 = fullDeck[0];
                            fullDeck.RemoveAt(0);
                            usedCards++;
                            playerTotal = (playerTotal + GetCardPoint(p3)) % 10;
                            detail += $" 他補:{p3}";

                            int bankerDraw = BankerDrawRule(bankerTotal, GetCardPoint(p3));
                            if (bankerDraw == 1 && fullDeck.Count > 0)
                            {
                                b3 = fullDeck[0];
                                fullDeck.RemoveAt(0);
                                usedCards++;
                                bankerTotal = (bankerTotal + GetCardPoint(b3)) % 10;
                                detail += $" 你補:{b3}";
                            }
                        }
                        else if (bankerTotal <= 5 && fullDeck.Count > 0)
                        {
                            b3 = fullDeck[0];
                            fullDeck.RemoveAt(0);
                            usedCards++;
                            bankerTotal = (bankerTotal + GetCardPoint(b3)) % 10;
                            detail += $" 你補:{b3}";
                        }
                    }

                    string winner = playerTotal > bankerTotal ? "P" : bankerTotal > playerTotal ? "B" : "T";
                    roundResults.Add((winner, detail));
                }

                string encoded = EncodeResult(roundResults);
                simulatedRecords.Add(encoded);

                string summary = $"模擬{i + 1}：{encoded}";
                allDetails.Add(summary);
            }

            // 更新畫面
            recordLabel.Text = $"最後一次模擬紀錄：{simulatedRecords[^1]}";
            detailLabel.Text = string.Join("\n", allDetails);

            // 儲存模擬紀錄 + 原有手動紀錄
            await SaveAllDataAsync(simulatedRecords);
        }
        
        
        // 🔘 按下「清除輸入」按鈕：清空手動輸入欄位
        private void OnClearManualInputClicked(object sender, EventArgs e)
        {
            manualInputEntry.Text = string.Empty;
        }


        // 🔘 按下「儲存手動紀錄」按鈕：把輸入欄位的資料存進 manualRecords 並寫入檔案
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


        // 🔘 按下「查看手動紀錄牌局」按鈕：從 AppDataDirectory 下的 baccaratData.json 讀取 ManualHistory 並顯示，載入 AppPackage 的原始資料 + AppDataDirectory 自訂資料
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



        // 🔁 頁面顯示時觸發：讀取 baccaratData.json 中的手動紀錄並寫入記憶
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


        // 💾 將模擬結果與手動紀錄一併儲存至 baccaratData.json
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
                        manualRecords.AddRange(data.ManualHistory); // ✅ 正確讀入手動紀錄
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("讀取錯誤", $"載入資料失敗：{ex.Message}", "好");
            }
        }








        // 🔧 根據莊家總點數與閒家第三張牌點數，決定莊家是否要補牌
        private int BankerDrawRule(int bankerTotal, int playerThird)
        {
            return bankerTotal switch
            {
                <= 2 => 1,
                3 => playerThird == 8 ? 0 : 1,
                4 => (playerThird >= 2 && playerThird <= 7) ? 1 : 0,
                5 => (playerThird >= 4 && playerThird <= 7) ? 1 : 0,
                6 => (playerThird == 6 || playerThird == 7) ? 1 : 0,
                _ => 0
            };
        }


        // 🔧 根據牌面文字回傳點數（JQK算0，其他為對應數字）
        private int GetCardPoint(string card)
        {
            string rank = card[1..];
            return rank switch
            {
                "A" => 1,
                "2" => 2,
                "3" => 3,
                "4" => 4,
                "5" => 5,
                "6" => 6,
                "7" => 7,
                "8" => 8,
                "9" => 9,
                _ => 0
            };
        }

        // 🔧 建立洗牌後的 8 副牌牌組（416 張牌），回傳洗好的完整牌堆
        private List<string> GenerateShuffledDeck()
        {
            List<string> suits = new() { "♠", "♥", "♦", "♣" };
            List<string> ranks = new() { "A", "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K" };
            List<string> deck = new();

            for (int d = 0; d < 8; d++)
            {
                foreach (string suit in suits)
                {
                    foreach (string rank in ranks)
                    {
                        deck.Add($"{suit}{rank}");
                    }
                }
            }

            for (int i = deck.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (deck[i], deck[j]) = (deck[j], deck[i]);
            }

            return deck;
        }



        // 🔧 將一場模擬結果壓縮成「連續同一方勝利次數」編碼字串（最多9）
        private string EncodeResult(List<(string winner, string detail)> results)
        {
            List<string> encoded = new();
            int count = 0;
            string current = null;

            foreach (var (winner, _) in results)
            {
                if (winner == "T") continue;
                if (winner != current)
                {
                    if (current != null)
                        encoded.Add(count > 9 ? "9" : count.ToString());
                    current = winner;
                    count = 1;
                }
                else
                {
                    count++;
                }
            }

            if (current != null)
                encoded.Add(count > 9 ? "9" : count.ToString());

            return string.Join("", encoded);
        }





    }





















    // 🔧 存檔用的資料格式（模擬紀錄 + 手動紀錄）
    public class DatingData
    {
        public List<string> BaccaratHistory { get; set; } = new(); // 模擬紀錄
        public List<string> ManualHistory { get; set; } = new();   // 手動紀錄
    }

}
