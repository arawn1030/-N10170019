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
        // �@�}�l�N�N�J�I�]�w�b�K�X���
        // ���ݵe�����J������A�]�w�J�I
        this.Loaded += (s, e) =>
        {
            passwordEntry.Focus();
        };
    }

    private void OnLoginClicked(object sender, EventArgs e)
    {
        string username = accountEntry.Text?.Trim();
        string password = passwordEntry.Text?.Trim();

        // �d�ҡG���]���T�b���O "aa"�A�K�X�O "1020"
        if (username != "aa")
        {
            accountErrorLabel.IsVisible = true; // ��ܬ��r
            return;
        }
        else
        {
            accountErrorLabel.IsVisible = false; // ���ì��r
        }

        if (password == "1020")
        {
            // �n�J���\�A�ɦV�D��
            Application.Current.MainPage = new AppShell(); // �� MainPage()
        }
        else
        {
            // �K�X���~�i�H�A�[�J���ܡ]�ثe�u���b�����~���ܡ^
        }
    }

}
