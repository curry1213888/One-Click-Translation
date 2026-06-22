using CtrlTranslator.App.Api;
using CtrlTranslator.App.Models;

namespace CtrlTranslator.App.Services;

public sealed class YoudaoTranslationService : ITranslationService
{
    private readonly YoudaoClient _client;
    private readonly AppSettings _settings;

    public YoudaoTranslationService(YoudaoClient client, AppSettings settings)
    {
        _client = client;
        _settings = settings;
    }

    public async Task<TranslationResult> TranslateAsync(string text, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new TranslationResult(text, string.Empty);
        }

        if (string.IsNullOrWhiteSpace(_settings.YoudaoAppKey) ||
            string.IsNullOrWhiteSpace(_settings.YoudaoAppSecret))
        {
            // 首次搭建时没有密钥，先给可见反馈，避免用户误以为无响应。
            return new TranslationResult(text, "请先在配置中填写有道 AppKey 与 AppSecret");
        }

        var translated = await _client.TranslateAsync(
            text,
            _settings.YoudaoAppKey,
            _settings.YoudaoAppSecret,
            _settings.SourceLanguage,
            _settings.TargetLanguage,
            cancellationToken);

        return new TranslationResult(text, translated);
    }
}
