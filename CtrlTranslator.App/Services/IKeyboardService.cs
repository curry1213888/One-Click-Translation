namespace CtrlTranslator.App.Services;

public interface IKeyboardService : IDisposable
{
    event EventHandler HotkeyTriggered;
    event EventHandler EscPressed;
    void Start();
}
