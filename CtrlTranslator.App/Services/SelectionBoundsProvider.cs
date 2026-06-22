using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Text;

namespace CtrlTranslator.App.Services;

public sealed class SelectionBoundsProvider
{
    private const int GuiCaretBlinking = 0x00000001;

    public bool TryGetSelectionBounds(out Rect bounds)
    {
        if (TryGetTextSelectionBounds(out bounds))
        {
            return true;
        }

        if (TryGetCaretBounds(out bounds))
        {
            return true;
        }

        if (TryGetCursorBounds(out bounds))
        {
            return true;
        }

        bounds = default;
        return false;
    }

    private static bool TryGetTextSelectionBounds(out Rect bounds)
    {
        bounds = default;

        try
        {
            var element = AutomationElement.FocusedElement;
            if (element?.TryGetCurrentPattern(TextPattern.Pattern, out var patternObject) != true ||
                patternObject is not TextPattern textPattern)
            {
                return false;
            }

            var ranges = textPattern.GetSelection();
            if (ranges is not { Length: > 0 })
            {
                return false;
            }

            return TryUnionBoundingRectangles(ranges, out bounds);
        }
        catch
        {
            return false;
        }
    }

    private static bool TryUnionBoundingRectangles(TextPatternRange[] ranges, out Rect bounds)
    {
        var minX = double.MaxValue;
        var minY = double.MaxValue;
        var maxX = double.MinValue;
        var maxY = double.MinValue;
        var hasBounds = false;

        foreach (var range in ranges)
        {
            var rectangles = range.GetBoundingRectangles();
            if (rectangles is not { Length: > 0 })
            {
                continue;
            }

            foreach (var rectangle in rectangles)
            {
                if (rectangle.Width <= 0 || rectangle.Height <= 0)
                {
                    continue;
                }

                hasBounds = true;
                minX = Math.Min(minX, rectangle.X);
                minY = Math.Min(minY, rectangle.Y);
                maxX = Math.Max(maxX, rectangle.Right);
                maxY = Math.Max(maxY, rectangle.Bottom);
            }
        }

        if (!hasBounds)
        {
            bounds = default;
            return false;
        }

        bounds = new Rect(minX, minY, maxX - minX, maxY - minY);
        return true;
    }

    private static bool TryGetCaretBounds(out Rect bounds)
    {
        bounds = default;

        var foregroundWindow = GetForegroundWindow();
        if (foregroundWindow == IntPtr.Zero)
        {
            return false;
        }

        var threadId = GetWindowThreadProcessId(foregroundWindow, out _);
        var threadInfo = new GuiThreadInfo
        {
            cbSize = Marshal.SizeOf<GuiThreadInfo>()
        };

        if (!GetGUIThreadInfo(threadId, ref threadInfo) ||
            threadInfo.hwndCaret == IntPtr.Zero ||
            (threadInfo.flags & GuiCaretBlinking) == 0)
        {
            return false;
        }

        var caret = threadInfo.rcCaret;
        var topLeft = new NativePoint { X = caret.Left, Y = caret.Top };
        var bottomRight = new NativePoint { X = caret.Right, Y = caret.Bottom };

        if (!ClientToScreen(threadInfo.hwndCaret, ref topLeft) ||
            !ClientToScreen(threadInfo.hwndCaret, ref bottomRight))
        {
            return false;
        }

        var width = bottomRight.X - topLeft.X;
        var height = bottomRight.Y - topLeft.Y;
        if (width <= 0)
        {
            width = 2;
        }

        if (height <= 0)
        {
            height = 16;
        }

        bounds = new Rect(topLeft.X, topLeft.Y, width, height);
        return true;
    }

    private static bool TryGetCursorBounds(out Rect bounds)
    {
        if (!GetCursorPos(out var point))
        {
            bounds = default;
            return false;
        }

        bounds = new Rect(point.X, point.Y, 0, 0);
        return true;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct GuiThreadInfo
    {
        public int cbSize;
        public int flags;
        public IntPtr hwndActive;
        public IntPtr hwndFocus;
        public IntPtr hwndCapture;
        public IntPtr hwndMenuOwner;
        public IntPtr hwndMoveSize;
        public IntPtr hwndCaret;
        public NativeRect rcCaret;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint _);

    [DllImport("user32.dll")]
    private static extern bool GetGUIThreadInfo(uint idThread, ref GuiThreadInfo lpgui);

    [DllImport("user32.dll")]
    private static extern bool ClientToScreen(IntPtr hWnd, ref NativePoint lpPoint);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out NativePoint lpPoint);
}
