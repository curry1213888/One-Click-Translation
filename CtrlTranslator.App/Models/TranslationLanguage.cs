namespace CtrlTranslator.App.Models;

public sealed class TranslationLanguage
{
    public required string Code { get; init; }
    public required string Name { get; init; }

    public override string ToString() => Name;
}

public static class TranslationLanguages
{
    public static IReadOnlyList<TranslationLanguage> Common { get; } =
    [
        new() { Code = "zh-CHS", Name = "简体中文" },
        new() { Code = "en", Name = "英语" },
        new() { Code = "ja", Name = "日语" },
        new() { Code = "ko", Name = "韩语" },
        new() { Code = "fr", Name = "法语" },
        new() { Code = "de", Name = "德语" },
        new() { Code = "es", Name = "西班牙语" },
        new() { Code = "ru", Name = "俄语" },
        new() { Code = "zh-CHT", Name = "繁体中文" },
        new() { Code = "pt", Name = "葡萄牙语" },
    ];

    public static string GetName(string code) =>
        Common.FirstOrDefault(language => language.Code == code)?.Name ?? code;
}
