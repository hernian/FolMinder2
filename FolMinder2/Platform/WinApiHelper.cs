using System;
using System.Collections.Generic;
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

        public static void SetForegroundWindow(Window window)
        {
            var hWnd = new WindowInteropHelper(window).EnsureHandle();
            SetForegroundWindow(hWnd);
        }
    }
}
