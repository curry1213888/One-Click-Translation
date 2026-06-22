using System.Windows.Forms;

namespace CtrlTranslator.App.Services;

public sealed class HotkeyBinding
{
    private static readonly HashSet<int> ModifierKeyCodes =
    [
        (int)Keys.ControlKey,
        (int)Keys.LControlKey,
        (int)Keys.RControlKey,
        (int)Keys.ShiftKey,
        (int)Keys.LShiftKey,
        (int)Keys.RShiftKey,
        (int)Keys.Menu,
        (int)Keys.LMenu,
        (int)Keys.RMenu
    ];

    public static HotkeyBinding Default { get; } = new() { Ctrl = true };

    public bool IsRightMouse { get; init; }
    public bool Ctrl { get; init; }
    public bool Shift { get; init; }
    public bool Alt { get; init; }
    public int? MainKeyCode { get; init; }

    public bool HasOnlyModifiers => !IsRightMouse && MainKeyCode is null;

    public static HotkeyBinding ParseOrDefault(string? raw)
    {
        return TryParse(raw, out var binding) ? binding : Default;
    }

    public static bool TryParse(string? raw, out HotkeyBinding binding)
    {
        binding = Default;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        var tokens = raw
            .Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0)
        {
            return false;
        }

        var isRightMouse = false;
        var ctrl = false;
        var shift = false;
        var alt = false;
        int? mainKey = null;

        foreach (var token in tokens)
        {
            if (token.Equals("RightMouse", StringComparison.OrdinalIgnoreCase))
            {
                if (tokens.Length > 1 || isRightMouse)
                {
                    return false;
                }
                isRightMouse = true;
                continue;
            }

            if (token.Equals("Ctrl", StringComparison.OrdinalIgnoreCase) ||
                token.Equals("Control", StringComparison.OrdinalIgnoreCase))
            {
                ctrl = true;
                continue;
            }

            if (token.Equals("Shift", StringComparison.OrdinalIgnoreCase))
            {
                shift = true;
                continue;
            }

            if (token.Equals("Alt", StringComparison.OrdinalIgnoreCase))
            {
                alt = true;
                continue;
            }

            if (!Enum.TryParse<Keys>(token, true, out var parsedKey))
            {
                return false;
            }

            var vkCode = (int)parsedKey;
            if (ModifierKeyCodes.Contains(vkCode) || mainKey.HasValue)
            {
                return false;
            }
            mainKey = vkCode;
        }

        if (!isRightMouse && !ctrl && !shift && !alt && !mainKey.HasValue)
        {
            return false;
        }

        binding = new HotkeyBinding
        {
            IsRightMouse = isRightMouse,
            Ctrl = ctrl,
            Shift = shift,
            Alt = alt,
            MainKeyCode = mainKey
        };
        return true;
    }

    public string ToConfigString()
    {
        if (IsRightMouse)
        {
            return "RightMouse";
        }

        var parts = new List<string>();
        if (Ctrl)
        {
            parts.Add("Ctrl");
        }
        if (Shift)
        {
            parts.Add("Shift");
        }
        if (Alt)
        {
            parts.Add("Alt");
        }
        if (MainKeyCode.HasValue)
        {
            parts.Add(((Keys)MainKeyCode.Value).ToString());
        }

        return parts.Count > 0 ? string.Join("+", parts) : Default.ToConfigString();
    }

    public string ToDisplayText()
    {
        if (IsRightMouse)
        {
            return "鼠标右键";
        }

        var parts = new List<string>();
        if (Ctrl)
        {
            parts.Add("Ctrl");
        }
        if (Shift)
        {
            parts.Add("Shift");
        }
        if (Alt)
        {
            parts.Add("Alt");
        }
        if (MainKeyCode.HasValue)
        {
            parts.Add(((Keys)MainKeyCode.Value).ToString().ToUpperInvariant());
        }

        return parts.Count > 0 ? string.Join("+", parts) : "Ctrl";
    }

    public bool MatchesModifiers(bool ctrlPressed, bool shiftPressed, bool altPressed)
    {
        return Ctrl == ctrlPressed && Shift == shiftPressed && Alt == altPressed;
    }
}
