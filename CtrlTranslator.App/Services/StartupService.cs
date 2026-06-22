using Microsoft.Win32;

namespace CtrlTranslator.App.Services;

public sealed class StartupService
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "CtrlTranslator";

    public bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, false);
        var value = key?.GetValue(AppName)?.ToString();
        return !string.IsNullOrWhiteSpace(value);
    }

    public void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, true) ??
                        Registry.CurrentUser.CreateSubKey(RunKey, true);
        if (key is null)
        {
            return;
        }

        if (enabled)
        {
            var executablePath = Environment.ProcessPath ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(executablePath))
            {
                key.SetValue(AppName, $"\"{executablePath}\"");
            }
        }
        else
        {
            key.DeleteValue(AppName, false);
        }
    }
}
