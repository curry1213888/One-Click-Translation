using System.Windows;

namespace CtrlTranslator.App.Services;

public interface IOverlayService
{
    void Show(string sourceText, string translatedText, Rect? selectionBounds = null);
    void Hide();
}
