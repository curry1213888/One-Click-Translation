using System.Windows;
using CtrlTranslator.App.Models;
using Application = System.Windows.Application;

namespace CtrlTranslator.App.Services;

public sealed class TranslationOrchestrator : IDisposable
{
    private readonly AppSettings _settings;
    private readonly IKeyboardService _keyboardService;
    private readonly ISelectionMonitorService _selectionMonitorService;
    private readonly IClipboardService _clipboardService;
    private readonly ITranslationService _translationService;
    private readonly IOverlayService _overlayService;
    private readonly SelectionBoundsProvider _selectionBoundsProvider;
    private CancellationTokenSource? _currentTranslationCts;
    private readonly EventHandler _escPressedHandler;
    private readonly EventHandler _selectionClearedHandler;

    public TranslationOrchestrator(
        AppSettings settings,
        IKeyboardService keyboardService,
        ISelectionMonitorService selectionMonitorService,
        IClipboardService clipboardService,
        ITranslationService translationService,
        IOverlayService overlayService)
    {
        _settings = settings;
        _keyboardService = keyboardService;
        _selectionMonitorService = selectionMonitorService;
        _clipboardService = clipboardService;
        _translationService = translationService;
        _overlayService = overlayService;
        _selectionBoundsProvider = new SelectionBoundsProvider();
        _escPressedHandler = (_, _) => HideOverlayOnUiThread();
        _selectionClearedHandler = (_, _) => HideOverlayOnUiThread();
    }

    public void Start()
    {
        _keyboardService.HotkeyTriggered += KeyboardServiceOnHotkeyTriggered;
        _keyboardService.EscPressed += _escPressedHandler;
        _selectionMonitorService.SelectionClearedLikely += _selectionClearedHandler;
        _keyboardService.Start();
        _selectionMonitorService.Start();
    }

    public void Dispose()
    {
        _keyboardService.HotkeyTriggered -= KeyboardServiceOnHotkeyTriggered;
        _keyboardService.EscPressed -= _escPressedHandler;
        _selectionMonitorService.SelectionClearedLikely -= _selectionClearedHandler;
        _keyboardService.Dispose();
        _selectionMonitorService.Dispose();
        _currentTranslationCts?.Cancel();
        _currentTranslationCts?.Dispose();
    }

    private async void KeyboardServiceOnHotkeyTriggered(object? sender, EventArgs e)
    {
        if (!_settings.AutoTranslateEnabled)
        {
            return;
        }

        try
        {
            _currentTranslationCts?.Cancel();
            _currentTranslationCts?.Dispose();
            _currentTranslationCts = new CancellationTokenSource();
            var token = _currentTranslationCts.Token;

            var selectedText = await _clipboardService.GetSelectedTextAsync(token);
            if (string.IsNullOrWhiteSpace(selectedText))
            {
                return;
            }

            Rect? selectionBounds = null;
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                if (_selectionBoundsProvider.TryGetSelectionBounds(out var bounds))
                {
                    selectionBounds = bounds;
                }
            });

            var result = await _translationService.TranslateAsync(selectedText, token);
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                _overlayService.Show(result.SourceText, result.TranslatedText, selectionBounds);
            });
        }
        catch (OperationCanceledException)
        {
            // 新请求覆盖旧请求时忽略即可。
        }
        catch (Exception ex)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                Rect? selectionBounds = null;
                if (_selectionBoundsProvider.TryGetSelectionBounds(out var bounds))
                {
                    selectionBounds = bounds;
                }

                _overlayService.Show("翻译失败", ex.Message, selectionBounds);
            });
        }
    }

    private void HideOverlayOnUiThread()
    {
        _ = Application.Current.Dispatcher.InvokeAsync(() => _overlayService.Hide());
    }
}
