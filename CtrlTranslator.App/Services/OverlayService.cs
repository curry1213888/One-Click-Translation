using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using CtrlTranslator.App.Views;
using Screen = System.Windows.Forms.Screen;

namespace CtrlTranslator.App.Services;

public sealed class OverlayService : IOverlayService
{
    private const double MarginDip = 8;
    private const double GapDip = 12;

    private readonly OverlayWindow _overlayWindow;
    private readonly SelectionBoundsProvider _selectionBoundsProvider;
    private readonly DispatcherTimer _autoHideTimer;

    public OverlayService(OverlayWindow overlayWindow)
    {
        _overlayWindow = overlayWindow;
        _selectionBoundsProvider = new SelectionBoundsProvider();
        _autoHideTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(10)
        };
        _autoHideTimer.Tick += (_, _) => Hide();
    }

    public void Show(string sourceText, string translatedText, Rect? selectionBounds = null)
    {
        _overlayWindow.SetText(sourceText, translatedText);
        PositionAboveSelection(selectionBounds);

        if (!_overlayWindow.IsVisible)
        {
            _overlayWindow.Show();
        }

        _autoHideTimer.Stop();
        _autoHideTimer.Start();
    }

    public void Hide()
    {
        _autoHideTimer.Stop();
        _overlayWindow.Hide();
    }

    private void PositionAboveSelection(Rect? selectionBounds)
    {
        _overlayWindow.UpdateLayout();

        var overlayWidth = _overlayWindow.ActualWidth > 0 ? _overlayWindow.ActualWidth : _overlayWindow.Width;
        var overlayHeight = _overlayWindow.ActualHeight > 0 ? _overlayWindow.ActualHeight : _overlayWindow.Height;
        var dpi = VisualTreeHelper.GetDpi(_overlayWindow);

        Rect bounds;
        if (selectionBounds.HasValue)
        {
            bounds = selectionBounds.Value;
        }
        else if (!_selectionBoundsProvider.TryGetSelectionBounds(out bounds))
        {
            return;
        }

        var selectionLeftDip = bounds.X / dpi.DpiScaleX;
        var selectionTopDip = bounds.Y / dpi.DpiScaleY;
        var selectionRightDip = bounds.Right / dpi.DpiScaleX;
        var selectionBottomDip = bounds.Bottom / dpi.DpiScaleY;
        var selectionCenterXDip = (selectionLeftDip + selectionRightDip) / 2;

        var anchorPoint = new System.Drawing.Point(
            (int)Math.Round(selectionCenterXDip * dpi.DpiScaleX),
            (int)Math.Round(selectionTopDip * dpi.DpiScaleY));
        var workArea = Screen.FromPoint(anchorPoint).WorkingArea;

        var workLeftDip = workArea.Left / dpi.DpiScaleX;
        var workTopDip = workArea.Top / dpi.DpiScaleY;
        var workRightDip = workArea.Right / dpi.DpiScaleX;
        var workBottomDip = workArea.Bottom / dpi.DpiScaleY;

        var left = selectionCenterXDip - overlayWidth / 2;
        var top = selectionTopDip - overlayHeight - GapDip;

        left = Math.Max(workLeftDip + MarginDip, Math.Min(left, workRightDip - overlayWidth - MarginDip));

        if (top < workTopDip + MarginDip)
        {
            top = selectionBottomDip + GapDip;
        }

        if (top + overlayHeight > workBottomDip - MarginDip)
        {
            top = workBottomDip - overlayHeight - MarginDip;
        }

        top = Math.Max(workTopDip + MarginDip, top);

        _overlayWindow.Left = left;
        _overlayWindow.Top = top;
    }
}
