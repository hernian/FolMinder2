using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;

namespace FolMinder2.Platform
{
    public static class WinApiHelper
    {
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;

            public int Width => Right - Left;
            public int Height => Bottom - Top;
        }

        public const uint SWP_NOSIZE = 0x0001;
        public const uint SWP_NOMOVE = 0x0002;
        public const uint SWP_NOZORDER = 0x0004;
        public const uint SWP_NOACTIVATE = 0x0010;
        public const uint SWP_SHOWWINDOW = 0x0040;
        public const uint SWP_HIDEWINDOW = 0x0080;

        public static readonly IntPtr HWND_TOP = new IntPtr(0);
        public static readonly IntPtr HWND_BOTTOM = new IntPtr(1);
        public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        public static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetWindowPos(
            IntPtr hWnd,
            IntPtr hWndInsertAfter,
            int X,
            int Y,
            int cx,
            int cy,
            uint uFlags);


        public static void SetForegroundWindow(Window window)
        {
            var hWnd = new WindowInteropHelper(window).EnsureHandle();
            SetForegroundWindow(hWnd);
        }

        public static RECT GetWindowRect(Window window)
        {
            var hWnd = new WindowInteropHelper(window).EnsureHandle();
            if (!GetWindowRect(hWnd, out RECT rect))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
            return rect;
        }

        public static void SetWindowPos(Window window, SetWindowPosParam p)
        {
            if (window == null) throw new ArgumentNullException(nameof(window));

            // WPF Window → HWND
            var hwnd = new WindowInteropHelper(window).EnsureHandle();

            // hWndInsertAfter の決定
            IntPtr insertAfter = p.ZOrder switch
            {
                ZOrderOption.Top => HWND_TOP,
                ZOrderOption.Bottom => HWND_BOTTOM,
                ZOrderOption.TopMost => HWND_TOPMOST,
                ZOrderOption.NoTopMost => HWND_NOTOPMOST,
                _ => IntPtr.Zero // Unchanged
            };

            // フラグ生成
            uint flags = 0;

            if (!p.ChangeSize)
                flags |= SWP_NOSIZE;

            if (!p.ChangePosition)
                flags |= SWP_NOMOVE;

            if (p.ZOrder == ZOrderOption.Unchanged)
                flags |= SWP_NOZORDER;

            if (!p.Activate)
                flags |= SWP_NOACTIVATE;

            flags |= p.Visibility switch
            {
                WindowVisibility.Show => SWP_SHOWWINDOW,
                WindowVisibility.Hide => SWP_HIDEWINDOW,
                _ => 0
            };

            // double → int（再現性のため Math.Round）
            int x = (int)Math.Round(p.Left);
            int y = (int)Math.Round(p.Top);
            int w = (int)Math.Round(p.Width);
            int h = (int)Math.Round(p.Height);

            SetWindowPos(hwnd, insertAfter, x, y, w, h, flags);
        }

    }
}
