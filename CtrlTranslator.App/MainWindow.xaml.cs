using System.Windows;
using System.Windows.Input;
using CtrlTranslator.App.ViewModels;

namespace CtrlTranslator.App;

public partial class MainWindow : Window
{
    private ModifierKeys? _pendingSingleModifier;

    public MainWindow()
    {
        InitializeComponent();
    }

    private MainViewModel? ViewModel => DataContext as MainViewModel;

    private void Window_OnPreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        var vm = ViewModel;
        if (vm is null || !vm.IsRecordingHotkey)
        {
            return;
        }

        e.Handled = true;
        var key = NormalizeKey(e);
        if (key == Key.Escape)
        {
            _pendingSingleModifier = null;
            vm.CancelHotkeyRecording();
            return;
        }

        if (TryToModifierKey(key, out var modifierKey))
        {
            var modifiers = Keyboard.Modifiers | modifierKey;
            if (CountModifiers(modifiers) >= 2)
            {
                _pendingSingleModifier = null;
                vm.ApplyCustomHotkey(FormatModifiers(modifiers));
                return;
            }

            _pendingSingleModifier = modifiers;
            return;
        }

        _pendingSingleModifier = null;
        vm.ApplyCustomHotkey(FormatHotkey(Keyboard.Modifiers, key));
    }

    private void Window_OnPreviewKeyUp(object sender, System.Windows.Input.KeyEventArgs e)
    {
        var vm = ViewModel;
        if (vm is null || !vm.IsRecordingHotkey || !_pendingSingleModifier.HasValue)
        {
            return;
        }

        var key = NormalizeKey(e);
        if (!TryToModifierKey(key, out _))
        {
            return;
        }

        if (Keyboard.Modifiers != ModifierKeys.None)
        {
            return;
        }

        e.Handled = true;
        var singleModifier = _pendingSingleModifier.Value;
        _pendingSingleModifier = null;
        vm.ApplyCustomHotkey(FormatModifiers(singleModifier));
    }

    private void Window_OnPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        var vm = ViewModel;
        if (vm is null || !vm.IsRecordingHotkey)
        {
            return;
        }

        e.Handled = true;
        _pendingSingleModifier = null;
        vm.ApplyCustomHotkey("RightMouse");
    }

    private static Key NormalizeKey(System.Windows.Input.KeyEventArgs e)
    {
        return e.Key == Key.System ? e.SystemKey : e.Key;
    }

    private static bool TryToModifierKey(Key key, out ModifierKeys modifierKeys)
    {
        switch (key)
        {
            case Key.LeftCtrl:
            case Key.RightCtrl:
                modifierKeys = ModifierKeys.Control;
                return true;
            case Key.LeftShift:
            case Key.RightShift:
                modifierKeys = ModifierKeys.Shift;
                return true;
            case Key.LeftAlt:
            case Key.RightAlt:
                modifierKeys = ModifierKeys.Alt;
                return true;
            default:
                modifierKeys = ModifierKeys.None;
                return false;
        }
    }

    private static int CountModifiers(ModifierKeys modifiers)
    {
        var count = 0;
        if (modifiers.HasFlag(ModifierKeys.Control))
        {
            count++;
        }
        if (modifiers.HasFlag(ModifierKeys.Shift))
        {
            count++;
        }
        if (modifiers.HasFlag(ModifierKeys.Alt))
        {
            count++;
        }
        return count;
    }

    private static string FormatModifiers(ModifierKeys modifiers)
    {
        var parts = new List<string>();
        if (modifiers.HasFlag(ModifierKeys.Control))
        {
            parts.Add("Ctrl");
        }
        if (modifiers.HasFlag(ModifierKeys.Shift))
        {
            parts.Add("Shift");
        }
        if (modifiers.HasFlag(ModifierKeys.Alt))
        {
            parts.Add("Alt");
        }
        return string.Join("+", parts);
    }

    private static string FormatHotkey(ModifierKeys modifiers, Key key)
    {
        var modifierText = FormatModifiers(modifiers);
        var keyText = key.ToString();
        return string.IsNullOrWhiteSpace(modifierText) ? keyText : $"{modifierText}+{keyText}";
    }
}