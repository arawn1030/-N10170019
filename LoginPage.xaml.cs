using System;
using Microsoft.Maui.Controls;

namespace NumberSearchApp;

public partial class LoginPage : ContentPage
{
    private const string CorrectUsername = "aa";
    private const string CorrectPassword = "1020";

    public LoginPage()
    {
        
        InitializeComponent();
        // 一開始就將焦點設定在密碼欄位
        // 等待畫面載入完成後再設定焦點
        this.Loaded += (s, e) =>
        {
            passwordEntry.Focus();
        };
    }

    private void OnLoginClicked(object sender, EventArgs e)
    {
        string username = accountEntry.Text?.Trim();
        string password = passwordEntry.Text?.Trim();

        // 範例：假設正確帳號是 "aa"，密碼是 "1020"
        if (username != "aa")
        {
            accountErrorLabel.IsVisible = true; // 顯示紅字
            return;
        }
        else
        {
            accountErrorLabel.IsVisible = false; // 隱藏紅字
        }

        if (password == "1020")
        {
            // 登入成功，導向主頁
            Application.Current.MainPage = new AppShell(); // 或 MainPage()
        }
        else
        {
            // 密碼錯誤可以再加入提示（目前只有帳號錯誤提示）
        }
    }

}
