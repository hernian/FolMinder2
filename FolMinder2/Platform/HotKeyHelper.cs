using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using FolMinder2.Models;

namespace FolMinder2.Platform
{
    public static class HotKeyHelper
    {
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        public const int WM_HOTKEY = 0x0312;
        public const uint MOD_ALT = 0x0001;
        public const uint MOD_CONTROL = 0x0002;
        public const uint MOD_SHIFT = 0x0004;
        public const uint MOD_WIN = 0x0008;
        public const uint MOD_NOREPEAT = 0x4000;

        public static void RegisterHotKey(Window window, int id, HotKey hotKey)
        {
            Debug.WriteLine($"RegisterHotKey. Alt: {hotKey.Alt}, Control: {hotKey.Control}, Shift: {hotKey.Shift}, Win: {hotKey.Win}, Key: {hotKey.Key}");
            var helper = new WindowInteropHelper(window);
            IntPtr hWnd = helper.Handle;

            if (hWnd == IntPtr.Zero)
            {
                Debug.WriteLine("ERROR: RegisterHotKey failed - Window handle is zero");
                return;
            }

            uint mods = 0;
            mods |= hotKey.Alt ? MOD_ALT : 0;
            mods |= hotKey.Control ? MOD_CONTROL : 0;
            mods |= hotKey.Shift ? MOD_SHIFT : 0;
            mods |= hotKey.Win ? MOD_WIN : 0;
            uint vk = (uint)KeyInterop.VirtualKeyFromKey(hotKey.Key);
            Debug.WriteLine($"RegisterHotKey. hWnd: {hWnd:x8}, mods: {mods:x4}, vk: {vk:x4}");

            var r = RegisterHotKey(hWnd, id, mods, vk);
            if (r)
            {
                Debug.WriteLine($"RegisterHotKey SUCCESS for ID: {id}");
            }
            else
            {
                var error = Marshal.GetLastWin32Error();
                Debug.WriteLine($"RegisterHotKey FAILED for ID: {id}, Error Code: {error}");
                // Error codes: 1409 = Hot key already registered by another app
                //              5 = Access denied
            }
        }

        public static void UnregisterHotKey(Window window, int id)
        {
            var helper = new WindowInteropHelper(window);
            IntPtr hWnd = helper.Handle;

            if (hWnd == IntPtr.Zero)
            {
                Debug.WriteLine("WARNING: UnregisterHotKey - Window handle is zero");
                return;
            }

            var r = UnregisterHotKey(hWnd, id);
            if (r)
            {
                Debug.WriteLine($"UnregisterHotKey SUCCESS for ID: {id}");
            }
            else
            {
                var error = Marshal.GetLastWin32Error();
                Debug.WriteLine($"UnregisterHotKey FAILED for ID: {id}, Error Code: {error}");
            }
        }
    }
}
