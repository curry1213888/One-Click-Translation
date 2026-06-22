using System.IO;
using System.Text.Json;
using CtrlTranslator.App.Models;

namespace CtrlTranslator.App.Services;

public sealed class LocalSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _settingsFilePath;

    public LocalSettingsStore()
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "CtrlTranslator");
        _settingsFilePath = Path.Combine(root, "settings.json");
    }

    public void Load(AppSettings settings)
    {
        if (!File.Exists(_settingsFilePath))
        {
            return;
        }

        try
        {
            var json = File.ReadAllText(_settingsFilePath);
            var dto = JsonSerializer.Deserialize<LocalSettingsDto>(json, JsonOptions);
            if (dto is null)
            {
                return;
            }

            settings.AutoTranslateEnabled = dto.AutoTranslateEnabled;
            settings.YoudaoAppKey = dto.YoudaoAppKey ?? string.Empty;
            settings.YoudaoAppSecret = dto.YoudaoAppSecret ?? string.Empty;
            settings.SourceLanguage = string.IsNullOrWhiteSpace(dto.SourceLanguage) ? "en" : dto.SourceLanguage;
            settings.TargetLanguage = string.IsNullOrWhiteSpace(dto.TargetLanguage) ? "zh-CHS" : dto.TargetLanguage;
            settings.TriggerHotkey = string.IsNullOrWhiteSpace(dto.TriggerHotkey) ? "Ctrl" : dto.TriggerHotkey;
            settings.MinimizeToTrayOnClose = dto.MinimizeToTrayOnClose;
        }
        catch
        {
            // 配置损坏时不影响主流程，沿用默认值启动。
        }
    }

    public void Save(AppSettings settings)
    {
        try
        {
            var directory = Path.GetDirectoryName(_settingsFilePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var dto = new LocalSettingsDto
            {
                AutoTranslateEnabled = settings.AutoTranslateEnabled,
                YoudaoAppKey = settings.YoudaoAppKey,
                YoudaoAppSecret = settings.YoudaoAppSecret,
                SourceLanguage = settings.SourceLanguage,
                TargetLanguage = settings.TargetLanguage,
                TriggerHotkey = settings.TriggerHotkey,
                MinimizeToTrayOnClose = settings.MinimizeToTrayOnClose
            };
            var json = JsonSerializer.Serialize(dto, JsonOptions);
            File.WriteAllText(_settingsFilePath, json);
        }
        catch
        {
            // 写入失败不抛异常，避免打断用户翻译流程。
        }
    }

    private sealed class LocalSettingsDto
    {
        public bool AutoTranslateEnabled { get; set; } = true;
        public string? YoudaoAppKey { get; set; }
        public string? YoudaoAppSecret { get; set; }
        public string? SourceLanguage { get; set; } = "en";
        public string? TargetLanguage { get; set; } = "zh-CHS";
        public string? TriggerHotkey { get; set; } = "Ctrl";
        public bool MinimizeToTrayOnClose { get; set; } = true;
    }
}
