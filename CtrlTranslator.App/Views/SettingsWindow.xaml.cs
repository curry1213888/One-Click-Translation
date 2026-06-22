using System.Windows;
using System.Windows.Media;
using CtrlTranslator.App.Api;
using Brushes = System.Windows.Media.Brushes;

namespace CtrlTranslator.App.Views;

public partial class SettingsWindow : Window
{
    private readonly YoudaoClient _youdaoClient;
    private bool _isBusy;

    public SettingsWindow(string appKey, string appSecret, YoudaoClient youdaoClient)
    {
        InitializeComponent();
        _youdaoClient = youdaoClient;
        AppKeyTextBox.Text = appKey;
        AppSecretPasswordBox.Password = appSecret;
    }

    public string YoudaoAppKey => AppKeyTextBox.Text.Trim();
    public string YoudaoAppSecret => AppSecretPasswordBox.Password.Trim();

    private async void SaveButton_OnClick(object sender, RoutedEventArgs e)
    {
        var ok = await ValidateConnectionAsync();
        if (!ok)
        {
            return;
        }

        DialogResult = true;
        Close();
    }

    private void CancelButton_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private async void TestConnectionButton_OnClick(object sender, RoutedEventArgs e)
    {
        await ValidateConnectionAsync();
    }

    private async Task<bool> ValidateConnectionAsync()
    {
        if (_isBusy)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(YoudaoAppKey) || string.IsNullOrWhiteSpace(YoudaoAppSecret))
        {
            SetResult("请先填写 AppKey 和 AppSecret。", false);
            return false;
        }

        try
        {
            SetBusy(true);
            SetResult("正在连接有道服务...", true, isPending: true);

            var translated = await _youdaoClient.TranslateAsync(
                "hello",
                YoudaoAppKey,
                YoudaoAppSecret,
                "en",
                "zh-CHS",
                CancellationToken.None);

            if (string.IsNullOrWhiteSpace(translated))
            {
                SetResult("连接失败：有道返回为空。", false);
                return false;
            }

            SetResult($"连接成功：hello -> {translated}", true);
            return true;
        }
        catch (Exception ex)
        {
            SetResult($"连接失败：{ex.Message}", false);
            return false;
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void SetBusy(bool busy)
    {
        _isBusy = busy;
        TestConnectionButton.IsEnabled = !busy;
        SaveButton.IsEnabled = !busy;
    }

    private void SetResult(string text, bool success, bool isPending = false)
    {
        TestResultTextBlock.Text = text;
        TestResultTextBlock.Foreground = isPending
            ? Brushes.DimGray
            : success
                ? Brushes.ForestGreen
                : Brushes.Firebrick;
    }
}
