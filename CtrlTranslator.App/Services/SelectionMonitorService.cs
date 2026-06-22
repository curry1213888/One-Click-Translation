using System.Diagnostics;
using System.Runtime.InteropServices;

namespace CtrlTranslator.App.Services;

public sealed class SelectionMonitorService : ISelectionMonitorService
{
    private const int WhMouseLl = 14;
    private const int WmLButtonDown = 0x0201;
    private const int WmRButtonDown = 0x0204;

    private readonly HookProc _hookCallback;
    private IntPtr _hookHandle = IntPtr.Zero;

    public event EventHandler? SelectionClearedLikely;

    public SelectionMonitorService()
    {
        _hookCallback = HookCallback;
    }

    public void Start()
    {
        if (_hookHandle != IntPtr.Zero)
        {
            return;
        }

        using var curProcess = Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule;
        _hookHandle = SetWindowsHookEx(WhMouseLl, _hookCallback, GetModuleHandle(curModule?.ModuleName), 0);
        if (_hookHandle == IntPtr.Zero)
        {
            throw new InvalidOperationException("无法安装全局鼠标钩子。");
        }
    }

    public void Dispose()
    {
        if (_hookHandle != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hookHandle);
            _hookHandle = IntPtr.Zero;
        }
        GC.SuppressFinalize(this);
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var msg = wParam.ToInt32();
            if (msg is WmLButtonDown or WmRButtonDown)
            {
                SelectionClearedLikely?.Invoke(this, EventArgs.Empty);
            }
        }

        return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
    }

    private delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);
}
