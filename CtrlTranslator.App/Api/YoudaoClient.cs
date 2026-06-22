using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CtrlTranslator.App.Api;

public sealed class YoudaoClient
{
    private const string ApiUrl = "https://openapi.youdao.com/api";
    private readonly HttpClient _httpClient = new();

    public async Task<string> TranslateAsync(
        string text,
        string appKey,
        string appSecret,
        string sourceLanguage,
        string targetLanguage,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var requestValues = BuildRequestValues(text, appKey, appSecret, sourceLanguage, targetLanguage);
        using var content = new FormUrlEncodedContent(requestValues);
        using var response = await _httpClient.PostAsync(ApiUrl, content, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"有道请求失败: {response.StatusCode}");
        }

        using var doc = JsonDocument.Parse(payload);
        if (doc.RootElement.TryGetProperty("errorCode", out var errorCodeNode))
        {
            var errorCode = errorCodeNode.GetString() ?? string.Empty;
            if (!string.Equals(errorCode, "0", StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"有道鉴权失败，错误码: {errorCode}");
            }
        }

        if (!doc.RootElement.TryGetProperty("translation", out var translationNode) ||
            translationNode.ValueKind != JsonValueKind.Array ||
            translationNode.GetArrayLength() == 0)
        {
            throw new InvalidOperationException("有道返回格式异常");
        }

        return translationNode[0].GetString() ?? string.Empty;
    }

    private static Dictionary<string, string> BuildRequestValues(
        string text,
        string appKey,
        string appSecret,
        string sourceLanguage,
        string targetLanguage)
    {
        var salt = Guid.NewGuid().ToString("N");
        var curTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var sign = ComputeSign(appKey, text, salt, curTime, appSecret);

        return new Dictionary<string, string>
        {
            ["q"] = text,
            ["from"] = sourceLanguage,
            ["to"] = targetLanguage,
            ["appKey"] = appKey,
            ["salt"] = salt,
            ["sign"] = sign,
            ["signType"] = "v3",
            ["curtime"] = curTime
        };
    }

    private static string ComputeSign(string appKey, string q, string salt, string curTime, string appSecret)
    {
        var input = $"{appKey}{Truncate(q)}{salt}{curTime}{appSecret}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string Truncate(string text)
    {
        if (text.Length <= 20)
        {
            return text;
        }

        return $"{text[..10]}{text.Length}{text[^10..]}";
    }
}
