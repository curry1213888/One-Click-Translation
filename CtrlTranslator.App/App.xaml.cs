using System.Drawing;
using System.Windows.Forms;
using System.Windows;
using CtrlTranslator.App.Api;
using CtrlTranslator.App.Models;
using CtrlTranslator.App.Services;
using CtrlTranslator.App.ViewModels;
using CtrlTranslator.App.Views;
using Application = System.Windows.Application;

namespace CtrlTranslator.App;

public partial class App : Application
{
    private NotifyIcon? _notifyIcon;
    private MainWindow? _mainWindow;
    private MainViewModel? _mainViewModel;
    private TranslationOrchestrator? _orchestrator;
    private YoudaoClient? _youdaoClient;
    private AppSettings? _settings;
    private LocalSettingsStore? _settingsStore;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _settingsStore = new LocalSettingsStore();
        _settings = new AppSettings();
        _settingsStore.Load(_settings);

        var overlayWindow = new OverlayWindow();
        var overlayService = new OverlayService(overlayWindow);
        var keyboardService = new GlobalKeyboardService(_settings);
        var selectionMonitorService = new SelectionMonitorService();
        var clipboardService = new ClipboardService();
        var startupService = new StartupService();
        _youdaoClient = new YoudaoClient();
        var translationService = new YoudaoTranslationService(_youdaoClient, _settings);

        _orchestrator = new TranslationOrchestrator(
            _settings,
            keyboardService,
            selectionMonitorService,
            clipboardService,
            translationService,
            overlayService);

        _mainViewModel = new MainViewModel(_settings, startupService);
        _mainWindow = new MainWindow
        {
            DataContext = _mainViewModel
        };
        _mainViewModel.OpenSettingsRequested += MainViewModelOnOpenSettingsRequested;
        _settings.PropertyChanged += SettingsOnPropertyChanged;

        _mainWindow.Closing += MainWindowOnClosing;

        InitializeTrayIcon(_settings);

        _mainWindow.Show();
        _orchestrator.Start();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_mainViewModel is not null)
        {
            _mainViewModel.OpenSettingsRequested -= MainViewModelOnOpenSettingsRequested;
        }
        if (_settings is not null)
        {
            _settings.PropertyChanged -= SettingsOnPropertyChanged;
        }
        _orchestrator?.Dispose();
        _notifyIcon?.Dispose();
        base.OnExit(e);
    }

    private void InitializeTrayIcon(AppSettings settings)
    {
        var iconStream = Application.GetResourceStream(new Uri("pack://application:,,,/app.ico"))?.Stream;
        var icon = iconStream is not null ? new Icon(iconStream) : SystemIcons.Information;

        _notifyIcon = new NotifyIcon
        {
            Text = "Ctrl Translator",
            Icon = icon,
            Visible = true,
            ContextMenuStrip = BuildTrayMenu(settings)
        };

        _notifyIcon.DoubleClick += (_, _) => ShowMainWindow();
    }

    private ContextMenuStrip BuildTrayMenu(AppSettings settings)
    {
        var menu = new ContextMenuStrip();
        var toggleItem = new ToolStripMenuItem("开启自动翻译")
        {
            Checked = settings.AutoTranslateEnabled,
            CheckOnClick = true
        };
        toggleItem.CheckedChanged += (_, _) => settings.AutoTranslateEnabled = toggleItem.Checked;

        var showItem = new ToolStripMenuItem("显示主界面");
        showItem.Click += (_, _) => ShowMainWindow();

        var exitItem = new ToolStripMenuItem("退出");
        exitItem.Click += (_, _) =>
        {
            if (_mainWindow is not null)
            {
                _mainWindow.Closing -= MainWindowOnClosing;
                _mainWindow.Close();
            }
            Current.Shutdown();
        };

        menu.Items.Add(toggleItem);
        menu.Items.Add(showItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(exitItem);
        return menu;
    }

    private void ShowMainWindow()
    {
        if (_mainWindow is null)
        {
            return;
        }

        _mainWindow.Show();
        _mainWindow.WindowState = WindowState.Normal;
        _mainWindow.Activate();
    }

    private void MainWindowOnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_mainWindow is null || _settings is null)
        {
            return;
        }

        if (!_settings.MinimizeToTrayOnClose)
        {
            _notifyIcon?.Dispose();
            Current.Shutdown();
            return;
        }

        e.Cancel = true;
        _mainWindow.Hide();
    }

    private void MainViewModelOnOpenSettingsRequested(object? sender, EventArgs e)
    {
        if (_mainWindow is null || _settings is null)
        {
            return;
        }

        if (_youdaoClient is null)
        {
            return;
        }

        var settingsWindow = new SettingsWindow(_settings.YoudaoAppKey, _settings.YoudaoAppSecret, _youdaoClient)
        {
            Owner = _mainWindow
        };
        var result = settingsWindow.ShowDialog();
        if (result == true)
        {
            _settings.YoudaoAppKey = settingsWindow.YoudaoAppKey;
            _settings.YoudaoAppSecret = settingsWindow.YoudaoAppSecret;
        }
    }

    private void SettingsOnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (_settings is null)
        {
            return;
        }

        _settingsStore?.Save(_settings);
    }
}

