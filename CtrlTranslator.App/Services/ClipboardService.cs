using System.Runtime.InteropServices;
using System.Windows;
using Application = System.Windows.Application;
using Clipboard = System.Windows.Clipboard;
using DataFormats = System.Windows.DataFormats;
using IDataObject = System.Windows.IDataObject;
using TextDataFormat = System.Windows.TextDataFormat;

namespace CtrlTranslator.App.Services;

public sealed class ClipboardService : IClipboardService
{
    private const int KeyeventfKeyup = 0x0002;
    private const byte VkControl = 0x11;
    private const byte CKey = 0x43;

    public async Task<string?> GetSelectedTextAsync(CancellationToken cancellationToken)
    {
        IDataObject? snapshot = null;
        try
        {
            snapshot = await RunOnUiThreadAsync(() =>
            {
                return Clipboard.ContainsData(DataFormats.Text) || Clipboard.ContainsData(DataFormats.UnicodeText)
                    ? Clipboard.GetDataObject()
                    : null;
            });
        }
        catch
        {
            // 剪贴板偶发占用时继续执行，避免中断整体翻译流程。
        }

        SimulateCtrlC();
        await Task.Delay(120, cancellationToken);

        string? selectedText = null;
        try
        {
            selectedText = await RunOnUiThreadAsync(() =>
            {
                try
                {
                    if (Clipboard.ContainsText(TextDataFormat.UnicodeText))
                    {
                        return Clipboard.GetText(TextDataFormat.UnicodeText);
                    }

                    if (Clipboard.ContainsText())
                    {
                        return Clipboard.GetText();
                    }
                }
                catch
                {
                    // 剪贴板无有效文本或数据损坏时视为无内容，静默跳过翻译。
                }

                return null;
            });
        }
        finally
        {
            if (snapshot is not null)
            {
                try
                {
                    await RunOnUiThreadAsync(() => Clipboard.SetDataObject(snapshot, true));
                }
                catch
                {
                    // 恢复失败时不阻塞用户继续使用软件。
                }
            }
        }

        return string.IsNullOrWhiteSpace(selectedText) ? null : selectedText.Trim();
    }

    private static async Task RunOnUiThreadAsync(Action action)
    {
        await Application.Current.Dispatcher.InvokeAsync(action);
    }

    private static async Task<T> RunOnUiThreadAsync<T>(Func<T> func)
    {
        return await Application.Current.Dispatcher.InvokeAsync(func);
    }

    private static void SimulateCtrlC()
    {
        keybd_event(VkControl, 0, 0, 0);
        keybd_event(CKey, 0, 0, 0);
        keybd_event(CKey, 0, KeyeventfKeyup, 0);
        keybd_event(VkControl, 0, KeyeventfKeyup, 0);
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);
}
