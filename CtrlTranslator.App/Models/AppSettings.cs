using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CtrlTranslator.App.Models;

public sealed class AppSettings : INotifyPropertyChanged
{
    private bool _autoTranslateEnabled = true;
    private bool _startWithWindows;
    private string _youdaoAppKey = string.Empty;
    private string _youdaoAppSecret = string.Empty;
    private string _sourceLanguage = "en";
    private string _targetLanguage = "zh-CHS";
    private string _triggerHotkey = "Ctrl";
    private bool _minimizeToTrayOnClose = true;

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool AutoTranslateEnabled
    {
        get => _autoTranslateEnabled;
        set => SetField(ref _autoTranslateEnabled, value);
    }

    public bool StartWithWindows
    {
        get => _startWithWindows;
        set => SetField(ref _startWithWindows, value);
    }

    public string YoudaoAppKey
    {
        get => _youdaoAppKey;
        set => SetField(ref _youdaoAppKey, value);
    }

    public string YoudaoAppSecret
    {
        get => _youdaoAppSecret;
        set => SetField(ref _youdaoAppSecret, value);
    }

    public string SourceLanguage
    {
        get => _sourceLanguage;
        set => SetField(ref _sourceLanguage, value);
    }

    public string TargetLanguage
    {
        get => _targetLanguage;
        set => SetField(ref _targetLanguage, value);
    }

    public string TriggerHotkey
    {
        get => _triggerHotkey;
        set => SetField(ref _triggerHotkey, value);
    }

    public bool MinimizeToTrayOnClose
    {
        get => _minimizeToTrayOnClose;
        set => SetField(ref _minimizeToTrayOnClose, value);
    }

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(field, value))
        {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
