namespace CtrlTranslator.App.Services;

public interface IClipboardService
{
    Task<string?> GetSelectedTextAsync(CancellationToken cancellationToken);
}
