using System.Text.Json;

namespace NumberSearchApp;

public partial class ProfitPage : ContentPage
{
    private List<string> allStreakStrings = new();

    public ProfitPage()
    {
        InitializeComponent();
        LoadStreakData();
    }

    private async void LoadStreakData()
    {
        try
        {
            using var stream = await FileSystem.OpenAppPackageFileAsync("baccaratData.json");
            using var reader = new StreamReader(stream);
            string defaultJson = await reader.ReadToEndAsync();
            var defaultData = JsonSerializer.Deserialize<BaccaratData>(defaultJson);

            if (defaultData?.BaccaratHistory != null)
                allStreakStrings.AddRange(defaultData.BaccaratHistory);

            string customPath = Path.Combine(FileSystem.AppDataDirectory, "baccaratData.json");
            if (File.Exists(customPath))
            {
                string customJson = await File.ReadAllTextAsync(customPath);
                var customData = JsonSerializer.Deserialize<BaccaratData>(customJson);

                if (customData?.BaccaratHistory != null)
                    allStreakStrings.AddRange(customData.BaccaratHistory);

                if (customData?.ManualHistory != null)
                    allStreakStrings.AddRange(customData.ManualHistory
                        .Where(s => !string.IsNullOrWhiteSpace(s) && s.All(char.IsDigit)));
            }

            allStreakStrings = allStreakStrings.Distinct().ToList();
        }
        catch (Exception ex)
        {
            await DisplayAlert("錯誤", $"資料載入失敗：{ex.Message}", "OK");
        }
    }

    // 平注法分析按鈕的事件處理函式
    private void OnFlatBetClicked(object sender, EventArgs e)
    {
        // 嘗試取得使用者輸入的本金與單注金額，若失敗則不執行後續邏輯
        if (!TryGetInputs(out int capital, out int bet)) return;

        // 初始化統計變數
        int profitCount = 0;           // 記錄賺錢的筆數（最終金額 > 本金）
        int lossCount = 0;             // 記錄虧損但未破產的筆數（0 < 最終金額 < 本金）
        int brokeCount = 0;            // 記錄破產的筆數（最終金額 ≤ 0）
        int maxProfit = int.MinValue;  // 紀錄目前最大獲利
        string bestStreak = "";        // 紀錄達成最大獲利的字串

        Random rnd = new();            // 建立亂數產生器，用於決定第一段是否贏

        // 對每一組百家樂路紙字串進行模擬
        foreach (var streak in allStreakStrings)
        {
            int balance = capital;     // 每組模擬都從原始本金開始
            bool broke = false;        // 是否破產的旗標

            // 決定第一段是否是你下注那邊（true 表示你贏第一段）
            bool betOnWinningSide = rnd.Next(2) == 0;

            int segmentIndex = 0;      // 字串中的段落索引（每一個數字為一段）

            foreach (var c in streak)
            {
                if (!char.IsDigit(c)) continue; // 忽略非數字字元（保險處理）

                int rounds = c - '0';  // 取得此段落的連續局數（數字轉為整數）

                // 判斷這段是否為你下注的那一邊
                // segmentIndex 為偶數表示與第一段相同邊，奇數為相反邊
                bool isWinSegment = (segmentIndex % 2 == 0) == betOnWinningSide;

                // 模擬這一段的每一局
                for (int i = 0; i < rounds; i++)
                {
                    // 若餘額不足下注，則標記破產並跳出
                    if (balance < bet)
                    {
                        broke = true;
                        break;
                    }

                    balance -= bet; // 扣除下注金額

                    if (isWinSegment)
                    {
                        // 若這段是贏的，則獲得雙倍回報（本金 + 獲利）
                        balance += bet * 2;
                    }
                    // 若輸了則什麼都不加（等於輸掉本金）
                }

                if (broke) break; // 若已破產，停止處理後續局數

                segmentIndex++; // 移動到下一段（換邊）
            }

            // 根據最終資金狀態分類統計
            if (balance > capital)
            {
                profitCount++; // 最終金額高於本金，視為獲利

                // 若本次為最大獲利，則記錄
                if ((balance - capital) > maxProfit)
                {
                    maxProfit = balance - capital;
                    bestStreak = streak;
                }
            }
            else if (balance > 0)
            {
                lossCount++; // 虧損但仍有剩餘資金
            }
            else
            {
                brokeCount++; // 已輸光或餘額為零，視為破產
            }
        }

        // 計算最大獲利所屬的區間（搭配黃金比例等級）
        string range = GetProfitRange(capital, capital + maxProfit);

        // 顯示分析結果
        ResultLabel.Text = $"平注分析：\n" +
                           $"總筆數：{allStreakStrings.Count}\n" +
                           $"獲利：{profitCount} 筆\n" +
                           $"虧損未破產：{lossCount} 筆\n" +
                           $"破產：{brokeCount} 筆\n\n" +
                           $"最大獲利字串：{bestStreak}\n" +
                           $"獲利金額:{maxProfit}, 區間:{range}";
    }



    private void OnMartingaleClicked(object sender, EventArgs e)
    {
        // 嘗試取得使用者輸入的本金與初始下注金額，若失敗則不執行後續邏輯
        if (!TryGetInputs(out int capital, out int baseBet)) return;

        // 初始化統計變數
        int profitCount = 0;           // 最終獲利的筆數（balance > capital）
        int lossCount = 0;             // 虧損但未破產（0 < balance < capital）
        int brokeCount = 0;            // 破產筆數（balance <= 0）
        int maxProfit = int.MinValue;  // 最大獲利金額
        string bestStreak = "";        // 最大獲利對應的路紙字串

        Random rnd = new();            // 建立亂數產生器，用於決定下注起始方向

        // 遍歷所有路紙字串
        foreach (var streak in allStreakStrings)
        {
            int balance = capital;     // 每組模擬從使用者輸入的本金開始
            bool broke = false;        // 是否破產的旗標
            int currentBet = baseBet;  // 當前下注金額（初始為 baseBet）

            // 決定第一段是否是下注的那一邊（true 表示你贏第一段）
            bool betOnWinningSide = rnd.Next(2) == 0;

            int segmentIndex = 0;      // 當前處理的段落索引（每個數字是一段）

            foreach (var c in streak)
            {
                if (!char.IsDigit(c)) continue; // 忽略非數字字元（保險處理）

                int rounds = c - '0';  // 此段連續局數（轉成整數）

                // 判斷目前段落是否為下注的那一邊
                bool isWinSegment = (segmentIndex % 2 == 0) == betOnWinningSide;

                // 模擬這一段的每一局
                for (int i = 0; i < rounds; i++)
                {
                    // 若餘額不足下注，則標記破產並跳出
                    if (balance < currentBet)
                    {
                        broke = true;
                        break;
                    }

                    balance -= currentBet; // 扣除下注金額

                    if (isWinSegment)
                    {
                        // 贏的話獲得雙倍回報
                        balance += currentBet * 2;

                        // 重置為原始下注金額（重新開始一輪）
                        currentBet = baseBet;
                    }
                    else
                    {
                        // 輸的話進行馬丁倍注（下注金額加倍）
                        currentBet *= 2;
                    }
                }

                if (broke) break; // 若破產則終止後續段落模擬

                segmentIndex++; // 換下一段
            }

            // 結算本組模擬結果，進行分類
            if (balance > capital)
            {
                profitCount++; // 賺錢了

                // 如果目前這組獲利是最大值，則更新記錄
                if ((balance - capital) > maxProfit)
                {
                    maxProfit = balance - capital;
                    bestStreak = streak;
                }
            }
            else if (balance > 0)
            {
                lossCount++; // 虧損但沒輸光
            }
            else
            {
                brokeCount++; // 已經輸光或餘額為 0，算破產
            }
        }

        // 計算最大獲利所屬區間（可搭配黃金比例顯示）
        string range = GetProfitRange(capital, capital + maxProfit);

        // 顯示統計結果
        ResultLabel.Text = $"馬丁格爾分析：\n" +
                           $"總筆數：{allStreakStrings.Count}\n" +
                           $"獲利：{profitCount} 筆\n" +
                           $"虧損未破產：{lossCount} 筆\n" +
                           $"破產：{brokeCount} 筆\n\n" +
                           $"最大獲利字串：{bestStreak}\n" +
                           $"獲利金額:{maxProfit}, 區間:{range}";
    }




    private string GetProfitRange(int capital, int finalAmount)
    {
        double ratio = (double)finalAmount / capital;

        if (ratio >= 2.0)
            return "極高獲利";
        else if (ratio >= 1.618)
            return "高獲利";
        else if (ratio >= 1.0)
            return "中獲利";
        else
            return "低或虧損";
    }


    private bool TryGetInputs(out int capital, out int bet)
    {
        bool valid1 = int.TryParse(CapitalEntry.Text, out capital);
        bool valid2 = int.TryParse(BetEntry.Text, out bet);
        if (!valid1 || !valid2 || capital <= 0 || bet <= 0)
        {
            DisplayAlert("錯誤", "請輸入有效的本金與下注金額", "OK");
            return false;
        }
        return true;
    }

    private class BaccaratData
    {
        public List<string>? BaccaratHistory { get; set; }
        public List<string>? ManualHistory { get; set; }
    }
}
