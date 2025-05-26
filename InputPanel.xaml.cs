using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;



namespace NumberSearchApp;

public partial class InputPanel : ContentPage
{

    private List<string> manualRecords = new(); // ��ʬ����C��
    public InputPanel()
	{
		InitializeComponent();
        _ = LoadAllDataAsync(); // ?? �s�W�G��l�Ʈ�Ū���x�s�ɮ�
    }

    // ?? ���U�u�M����J�v���s�G�M�Ť�ʿ�J���
    private void OnClearManualInputClicked(object sender, EventArgs e)
    {
        manualInputEntry.Text = string.Empty;
    }


    // ?? ���U�u�x�s��ʬ����v���s�G���J��쪺��Ʀs�i manualRecords �üg�J�ɮ�
    private async void OnSaveManualRecordClicked(object sender, EventArgs e)
    {
        string input = manualInputEntry.Text?.Trim();
        if (!string.IsNullOrEmpty(input))
        {
            manualRecords.Add(input);
            await SaveAllDataAsync(new List<string>()); // �x�s�i�ɮ�
            await DisplayAlert("�w�x�s", $"�w�x�s�P���G{input}", "�n");
            manualInputEntry.Text = string.Empty;
        }
        else
        {
            await DisplayAlert("���~", "�Х���J�P�����e", "�n");
        }
    }

    // ?? ���U�u�d�ݤ�ʬ����P���v���s�G�q AppDataDirectory �U�� baccaratData.json Ū�� ManualHistory ����ܡA���J AppPackage ����l��� + AppDataDirectory �ۭq���
    private async void OnShowManualRecordsClicked(object sender, EventArgs e)
    {
        try
        {
            var allManualRecords = new List<string>();

            // �Ĥ@�B�GŪ���w�]���ظ�ơ]Resources/Raw�^
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
            // �ĤG�B�GŪ�� AppDataDirectory ���ۭq���
            string filePath = Path.Combine(FileSystem.AppDataDirectory, "baccaratData.json");
            if (File.Exists(filePath))
            {
                string customJson = await File.ReadAllTextAsync(filePath);
                var customData = JsonSerializer.Deserialize<DatingData>(customJson);

                if (customData?.ManualHistory != null)
                    allManualRecords.AddRange(customData.ManualHistory);
            }

            // �ĤT�B�G��s��ܡ]�h�����ơ^
            manualRecords = allManualRecords.Distinct().ToList();

            if (manualRecords.Count == 0)
                manualRecordsLabel.Text = "��ʬ����G�|�L�O��";
            else
                manualRecordsLabel.Text = "��ʬ����G\n" + string.Join("\n", manualRecords);
        }
        catch (Exception ex)
        {
            manualRecordsLabel.Text = $"Ū����ʬ����ɵo�Ϳ��~�G{ex.Message}";
        }
    }



    // ?? ������ܮ�Ĳ�o�GŪ�� baccaratData.json ������ʬ����üg�J�O��
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


    // ?? �N�������G�P��ʬ����@���x�s�� baccaratData.json
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

            // �[�J�ثe�Ȧs����ʬ����]�Ѥ�ʿ�J�϶����@�^
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
            await DisplayAlert("�x�s����", $"���������x�s���ѡG{ex.Message}", "�n");
        }
    }

    //���J�Ҧ���ƨç�s manualRecords �M��
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
                    manualRecords.Clear(); // �M���ª�
                    manualRecords.AddRange(data.ManualHistory); // ? ���TŪ�J��ʬ���
                }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ū�����~", $"���J��ƥ��ѡG{ex.Message}", "�n");
        }
    }

}