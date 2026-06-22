using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace CtrlTranslator.App.Views;

public partial class OverlayWindow : Window
{
    private const int GwlExstyle = -20;
    private const int WsExToolwindow = 0x00000080;
    private const int WsExNoactivate = 0x08000000;
    private const int WsExTransparent = 0x00000020;

    public OverlayWindow()
    {
        InitializeComponent();
    }

    public void SetText(string sourceText, string translatedText)
    {
        SourceTextBlock.Text = sourceText;
        TranslatedTextBlock.Text = translatedText;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        var handle = new WindowInteropHelper(this).Handle;
        var exStyle = GetWindowLong(handle, GwlExstyle);
        SetWindowLong(handle, GwlExstyle, exStyle | WsExToolwindow | WsExNoactivate | WsExTransparent);
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
}
