using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using CtrlTranslator.App.Models;

namespace CtrlTranslator.App.Services;

public sealed class GlobalKeyboardService : IKeyboardService
{
    private const int WhKeyboardLl = 13;
    private const int WhMouseLl = 14;
    private const int WmKeyDown = 0x0100;
    private const int WmKeyUp = 0x0101;
    private const int WmSysKeyDown = 0x0104;
    private const int WmSysKeyUp = 0x0105;
    private const int WmRButtonDown = 0x0204;
    private const int VkControl = 0x11;
    private const int VkLControl = 0xA2;
    private const int VkRControl = 0xA3;
    private const int VkShift = 0x10;
    private const int VkLShift = 0xA0;
    private const int VkRShift = 0xA1;
    private const int VkAlt = 0x12;
    private const int VkLAlt = 0xA4;
    private const int VkRAlt = 0xA5;
    private const int VkEscape = 0x1B;

    private readonly AppSettings _settings;
    private readonly KeyboardHookProc _keyboardHookCallback;
    private readonly MouseHookProc _mouseHookCallback;
    private readonly HashSet<int> _pressedNonModifierKeys = [];
    private IntPtr _keyboardHookHandle = IntPtr.Zero;
    private IntPtr _mouseHookHandle = IntPtr.Zero;
    private bool _ctrlPressed;
    private bool _shiftPressed;
    private bool _altPressed;
    private bool _modifierChordArmed;
    private bool _modifierChordInterfered;
    private HotkeyBinding? _armedBinding;

    public event EventHandler? HotkeyTriggered;
    public event EventHandler? EscPressed;

    public GlobalKeyboardService(AppSettings settings)
    {
        _settings = settings;
        _keyboardHookCallback = KeyboardHookCallback;
        _mouseHookCallback = MouseHookCallback;
    }

    public void Start()
    {
        if (_keyboardHookHandle != IntPtr.Zero)
        {
            return;
        }

        using var curProcess = Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule;
        _keyboardHookHandle = SetWindowsHookEx(WhKeyboardLl, _keyboardHookCallback, GetModuleHandle(curModule?.ModuleName), 0);
        _mouseHookHandle = SetWindowsHookEx(WhMouseLl, _mouseHookCallback, GetModuleHandle(curModule?.ModuleName), 0);
        if (_keyboardHookHandle == IntPtr.Zero || _mouseHookHandle == IntPtr.Zero)
        {
            throw new InvalidOperationException("无法安装全局输入钩子。");
        }
    }

    public void Dispose()
    {
        if (_keyboardHookHandle != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_keyboardHookHandle);
            _keyboardHookHandle = IntPtr.Zero;
        }

        if (_mouseHookHandle != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_mouseHookHandle);
            _mouseHookHandle = IntPtr.Zero;
        }
        GC.SuppressFinalize(this);
    }

    private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode < 0)
        {
            return CallNextHookEx(_keyboardHookHandle, nCode, wParam, lParam);
        }

        var msg = wParam.ToInt32();
        var keyInfo = Marshal.PtrToStructure<KbdLlHookStruct>(lParam);
        var vkCode = keyInfo.vkCode;

        if (msg is WmKeyDown or WmSysKeyDown)
        {
            HandleKeyDown(vkCode);
        }
        else if (msg is WmKeyUp or WmSysKeyUp)
        {
            HandleKeyUp(vkCode);
        }

        return CallNextHookEx(_keyboardHookHandle, nCode, wParam, lParam);
    }

    private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && wParam.ToInt32() == WmRButtonDown)
        {
            var binding = HotkeyBinding.ParseOrDefault(_settings.TriggerHotkey);
            if (binding.IsRightMouse)
            {
                HotkeyTriggered?.Invoke(this, EventArgs.Empty);
            }
        }

        return CallNextHookEx(_mouseHookHandle, nCode, wParam, lParam);
    }

    private void HandleKeyDown(int vkCode)
    {
        if (IsCtrl(vkCode))
        {
            _ctrlPressed = true;
            UpdateModifierChordState(vkCode, keyDown: true);
            return;
        }

        if (IsShift(vkCode))
        {
            _shiftPressed = true;
            UpdateModifierChordState(vkCode, keyDown: true);
            return;
        }

        if (IsAlt(vkCode))
        {
            _altPressed = true;
            UpdateModifierChordState(vkCode, keyDown: true);
            return;
        }

        if (vkCode == VkEscape)
        {
            EscPressed?.Invoke(this, EventArgs.Empty);
        }

        var firstDown = _pressedNonModifierKeys.Add(vkCode);
        if (!firstDown)
        {
            return;
        }

        if (_modifierChordArmed)
        {
            _modifierChordInterfered = true;
        }

        var binding = HotkeyBinding.ParseOrDefault(_settings.TriggerHotkey);
        if (binding.IsRightMouse || binding.HasOnlyModifiers || binding.MainKeyCode != vkCode)
        {
            return;
        }

        if (binding.MatchesModifiers(_ctrlPressed, _shiftPressed, _altPressed))
        {
            HotkeyTriggered?.Invoke(this, EventArgs.Empty);
        }
    }

    private void HandleKeyUp(int vkCode)
    {
        var binding = HotkeyBinding.ParseOrDefault(_settings.TriggerHotkey);

        if (IsCtrl(vkCode))
        {
            TriggerIfModifierChordCompletes(binding, vkCode);
            _ctrlPressed = false;
            UpdateModifierChordState(vkCode, keyDown: false);
            return;
        }

        if (IsShift(vkCode))
        {
            TriggerIfModifierChordCompletes(binding, vkCode);
            _shiftPressed = false;
            UpdateModifierChordState(vkCode, keyDown: false);
            return;
        }

        if (IsAlt(vkCode))
        {
            TriggerIfModifierChordCompletes(binding, vkCode);
            _altPressed = false;
            UpdateModifierChordState(vkCode, keyDown: false);
            return;
        }

        _pressedNonModifierKeys.Remove(vkCode);
    }

    private void UpdateModifierChordState(int vkCode, bool keyDown)
    {
        var binding = HotkeyBinding.ParseOrDefault(_settings.TriggerHotkey);
        if (binding.IsRightMouse || !binding.HasOnlyModifiers)
        {
            ResetModifierChord();
            return;
        }

        if (_pressedNonModifierKeys.Count > 0 || HasExtraModifier(binding))
        {
            _modifierChordInterfered = true;
        }

        if (binding.MatchesModifiers(_ctrlPressed, _shiftPressed, _altPressed) && keyDown)
        {
            _modifierChordArmed = true;
            _modifierChordInterfered = false;
            _armedBinding = binding;
        }

        if (!_ctrlPressed && !_shiftPressed && !_altPressed)
        {
            ResetModifierChord();
        }
    }

    private void TriggerIfModifierChordCompletes(HotkeyBinding binding, int releasedKeyCode)
    {
        if (!_modifierChordArmed || _modifierChordInterfered || _armedBinding is null)
        {
            return;
        }

        if (!_armedBinding.HasOnlyModifiers || !_armedBinding.MatchesModifiers(_ctrlPressed, _shiftPressed, _altPressed))
        {
            return;
        }

        if (!IsRequiredModifierKey(_armedBinding, releasedKeyCode))
        {
            return;
        }

        if (_pressedNonModifierKeys.Count == 0 && binding.ToConfigString() == _armedBinding.ToConfigString())
        {
            HotkeyTriggered?.Invoke(this, EventArgs.Empty);
        }
    }

    private void ResetModifierChord()
    {
        _modifierChordArmed = false;
        _modifierChordInterfered = false;
        _armedBinding = null;
    }

    private bool HasExtraModifier(HotkeyBinding binding)
    {
        return (_ctrlPressed && !binding.Ctrl) ||
               (_shiftPressed && !binding.Shift) ||
               (_altPressed && !binding.Alt);
    }

    private static bool IsRequiredModifierKey(HotkeyBinding binding, int vkCode)
    {
        return (binding.Ctrl && IsCtrl(vkCode)) ||
               (binding.Shift && IsShift(vkCode)) ||
               (binding.Alt && IsAlt(vkCode));
    }

    private static bool IsCtrl(int vkCode) => vkCode is VkControl or VkLControl or VkRControl;
    private static bool IsShift(int vkCode) => vkCode is VkShift or VkLShift or VkRShift;
    private static bool IsAlt(int vkCode) => vkCode is VkAlt or VkLAlt or VkRAlt;

    private delegate IntPtr KeyboardHookProc(int nCode, IntPtr wParam, IntPtr lParam);
    private delegate IntPtr MouseHookProc(int nCode, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct KbdLlHookStruct
    {
        public int vkCode;
        public int scanCode;
        public int flags;
        public int time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MsLlHookStruct
    {
        public int x;
        public int y;
        public int mouseData;
        public int flags;
        public int time;
        public IntPtr dwExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, KeyboardHookProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, MouseHookProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);
}
