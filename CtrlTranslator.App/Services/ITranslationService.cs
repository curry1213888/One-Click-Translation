using CtrlTranslator.App.Models;

namespace CtrlTranslator.App.Services;

public interface ITranslationService
{
    Task<TranslationResult> TranslateAsync(string text, CancellationToken cancellationToken);
}
