namespace CtrlTranslator.App.Services;

public interface ISelectionMonitorService : IDisposable
{
    event EventHandler SelectionClearedLikely;
    void Start();
}
