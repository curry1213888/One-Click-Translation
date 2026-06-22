using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CtrlTranslator.App.Models;
using CtrlTranslator.App.Services;

namespace CtrlTranslator.App.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private const string CustomHotkeyOptionId = "custom";

    private readonly AppSettings _settings;
    private readonly StartupService _startupService;
    private bool _startWithWindows;
    private string _selectedHotkeyOptionId = "ctrl";
    private bool _isRecordingHotkey;

    public MainViewModel(AppSettings settings, StartupService startupService)
    {
        _settings = settings;
        _startupService = startupService;
        _startWithWindows = _startupService.IsEnabled();
        _settings.StartWithWindows = _startWithWindows;
        _settings.PropertyChanged += SettingsOnPropertyChanged;
        OpenSettingsCommand = new RelayCommand(() => OpenSettingsRequested?.Invoke(this, EventArgs.Empty));
        SwapLanguagesCommand = new RelayCommand(SwapLanguages);
        StartHotkeyRecordingCommand = new RelayCommand(BeginCustomHotkeyRecording, () => IsCustomHotkeySelected && !_isRecordingHotkey);
        HotkeyOptions =
        [
            new() { Id = "ctrl", Label = "Ctrl", HotkeyValue = "Ctrl" },
            new() { Id = "ctrlShift", Label = "Ctrl+Shift", HotkeyValue = "Ctrl+Shift" },
            new() { Id = "alt", Label = "Alt", HotkeyValue = "Alt" },
            new() { Id = "ctrlAlt", Label = "Ctrl+Alt", HotkeyValue = "Ctrl+Alt" },
            new() { Id = "rightMouse", Label = "鼠标右键", HotkeyValue = "RightMouse" },
            new() { Id = CustomHotkeyOptionId, Label = "自定义", HotkeyValue = string.Empty, IsCustom = true }
        ];
        _settings.TriggerHotkey = HotkeyBinding.ParseOrDefault(_settings.TriggerHotkey).ToConfigString();
        SyncSelectedHotkeyOption(_settings.TriggerHotkey);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? OpenSettingsRequested;

    public ICommand OpenSettingsCommand { get; }
    public ICommand SwapLanguagesCommand { get; }
    public ICommand StartHotkeyRecordingCommand { get; }

    public IReadOnlyList<TranslationLanguage> Languages => TranslationLanguages.Common;
    public IReadOnlyList<HotkeyOption> HotkeyOptions { get; }

    public bool AutoTranslateEnabled
    {
        get => _settings.AutoTranslateEnabled;
        set
        {
            if (_settings.AutoTranslateEnabled == value)
            {
                return;
            }

            _settings.AutoTranslateEnabled = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AutoTranslateEnabled)));
        }
    }

    public bool StartWithWindows
    {
        get => _startWithWindows;
        set
        {
            if (_startWithWindows == value)
            {
                return;
            }

            _startWithWindows = value;
            _settings.StartWithWindows = value;
            _startupService.SetEnabled(value);
            OnPropertyChanged();
        }
    }

    public string SourceLanguage
    {
        get => _settings.SourceLanguage;
        set
        {
            if (_settings.SourceLanguage == value)
            {
                return;
            }

            _settings.SourceLanguage = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(LanguageDirection));
        }
    }

    public string TargetLanguage
    {
        get => _settings.TargetLanguage;
        set
        {
            if (_settings.TargetLanguage == value)
            {
                return;
            }

            _settings.TargetLanguage = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(LanguageDirection));
        }
    }

    public string LanguageDirection =>
        $"{TranslationLanguages.GetName(SourceLanguage)} → {TranslationLanguages.GetName(TargetLanguage)}";

    public string TriggerHotkeyDisplay => HotkeyBinding.ParseOrDefault(_settings.TriggerHotkey).ToDisplayText();

    public string TriggerHotkeyHintText =>
        $"按下 {TriggerHotkeyDisplay} 时自动翻译选中文字";

    public bool MinimizeToTrayOnClose
    {
        get => _settings.MinimizeToTrayOnClose;
        set
        {
            if (_settings.MinimizeToTrayOnClose == value)
            {
                return;
            }

            _settings.MinimizeToTrayOnClose = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CloseBehaviorDescription));
        }
    }

    public string CloseBehaviorDescription =>
        MinimizeToTrayOnClose
            ? "开启后点击关闭将最小化到托盘"
            : "关闭后点击关闭将直接退出应用";

    public bool IsRecordingHotkey
    {
        get => _isRecordingHotkey;
        private set
        {
            if (_isRecordingHotkey == value)
            {
                return;
            }

            _isRecordingHotkey = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HotkeyStatusText));
            (StartHotkeyRecordingCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }
    }

    public string HotkeyStatusText =>
        IsRecordingHotkey ? "请按下快捷键（支持组合键或鼠标右键）..." : $"当前快捷键：{TriggerHotkeyDisplay}";

    public string SelectedHotkeyOptionId
    {
        get => _selectedHotkeyOptionId;
        set
        {
            if (_selectedHotkeyOptionId == value)
            {
                return;
            }

            _selectedHotkeyOptionId = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsCustomHotkeySelected));
            (StartHotkeyRecordingCommand as RelayCommand)?.RaiseCanExecuteChanged();

            var option = HotkeyOptions.FirstOrDefault(item => item.Id == value);
            if (option is null || option.IsCustom)
            {
                return;
            }

            IsRecordingHotkey = false;
            _settings.TriggerHotkey = option.HotkeyValue;
            OnPropertyChanged(nameof(TriggerHotkeyDisplay));
            OnPropertyChanged(nameof(TriggerHotkeyHintText));
            OnPropertyChanged(nameof(HotkeyStatusText));
        }
    }

    public bool IsCustomHotkeySelected => SelectedHotkeyOptionId == CustomHotkeyOptionId;

    private void SwapLanguages()
    {
        var source = _settings.SourceLanguage;
        var target = _settings.TargetLanguage;
        _settings.SourceLanguage = target;
        _settings.TargetLanguage = source;
        OnPropertyChanged(nameof(SourceLanguage));
        OnPropertyChanged(nameof(TargetLanguage));
        OnPropertyChanged(nameof(LanguageDirection));
    }

    public void BeginCustomHotkeyRecording()
    {
        if (!IsCustomHotkeySelected)
        {
            SelectedHotkeyOptionId = CustomHotkeyOptionId;
        }

        IsRecordingHotkey = true;
    }

    public void ApplyCustomHotkey(string hotkeyValue)
    {
        if (!HotkeyBinding.TryParse(hotkeyValue, out var binding))
        {
            return;
        }

        _settings.TriggerHotkey = binding.ToConfigString();
        IsRecordingHotkey = false;
        SelectedHotkeyOptionId = CustomHotkeyOptionId;
        OnPropertyChanged(nameof(TriggerHotkeyDisplay));
        OnPropertyChanged(nameof(TriggerHotkeyHintText));
        OnPropertyChanged(nameof(HotkeyStatusText));
    }

    public void CancelHotkeyRecording()
    {
        IsRecordingHotkey = false;
    }

    private void SyncSelectedHotkeyOption(string hotkeyValue)
    {
        var matched = HotkeyOptions
            .FirstOrDefault(option => !option.IsCustom &&
                                      string.Equals(option.HotkeyValue, hotkeyValue, StringComparison.OrdinalIgnoreCase));
        _selectedHotkeyOptionId = matched?.Id ?? CustomHotkeyOptionId;
        OnPropertyChanged(nameof(SelectedHotkeyOptionId));
        OnPropertyChanged(nameof(IsCustomHotkeySelected));
        (StartHotkeyRecordingCommand as RelayCommand)?.RaiseCanExecuteChanged();
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void SettingsOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(AppSettings.AutoTranslateEnabled):
                OnPropertyChanged(nameof(AutoTranslateEnabled));
                break;
            case nameof(AppSettings.SourceLanguage):
                OnPropertyChanged(nameof(SourceLanguage));
                OnPropertyChanged(nameof(LanguageDirection));
                break;
            case nameof(AppSettings.TargetLanguage):
                OnPropertyChanged(nameof(TargetLanguage));
                OnPropertyChanged(nameof(LanguageDirection));
                break;
            case nameof(AppSettings.TriggerHotkey):
                _settings.TriggerHotkey = HotkeyBinding.ParseOrDefault(_settings.TriggerHotkey).ToConfigString();
                SyncSelectedHotkeyOption(_settings.TriggerHotkey);
                OnPropertyChanged(nameof(TriggerHotkeyDisplay));
                OnPropertyChanged(nameof(TriggerHotkeyHintText));
                OnPropertyChanged(nameof(HotkeyStatusText));
                break;
            case nameof(AppSettings.MinimizeToTrayOnClose):
                OnPropertyChanged(nameof(MinimizeToTrayOnClose));
                OnPropertyChanged(nameof(CloseBehaviorDescription));
                break;
        }
    }
}
