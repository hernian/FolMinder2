using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Windows;
using FolMinder2.Models;
using FolMinder2.Platform;

namespace FolMinder2.Services
{
    public interface IHotKeyService
    {
        event EventHandler? HotKeyPressed;
        void Initialize(Window window, int hotKeyId);
        void UpdateHotKey(HotKey newHotKey);
        void Shutdown();
        bool ProcessWindowMessage(int msg, IntPtr wParam);
    }

    public class HotKeyService : IHotKeyService
    {
        private Window? _window;
        private int _hotKeyId;
        private bool _isRegistered = false;

        public const int WM_HOTKEY = HotKeyHelper.WM_HOTKEY;

        /// <summary>
        /// HotKeyが押されたときに発火するイベント
        /// </summary>
        public event EventHandler? HotKeyPressed;

        public HotKeyService()
        {
        }

        /// <summary>
        /// ウィンドウを登録してHotKeyを初期化
        /// MainWindowのSourceInitializedイベントで呼び出される
        /// </summary>
        /// <param name="window">登録先のウィンドウ</param>
        public void Initialize(Window window, int hotKeyId)
        {
            if (_window != null)
            {
                throw new InvalidOperationException("HotKeyServiceは既に初期化されています。");
            }

            _window = window;
            _hotKeyId = hotKeyId;
            Debug.WriteLine($"HotKeyService.Initialize: ウィンドウ登録完了");
        }

        /// <summary>
        /// HotKeyを更新（解除 → 登録 → 保存）
        /// </summary>
        /// <param name="hotKey">新しいHotKey</param>
        public void UpdateHotKey(HotKey hotKey)
        {
            if (_window == null)
            {
                throw new InvalidOperationException("ウィンドウが登録されていません。Initialize()を先に呼び出してください。");
            }

            RegisterCurrentHotKey(hotKey);
            Debug.WriteLine($"HotKeyService.UpdateHotKey: Alt={hotKey.Alt}"
                + $", Control={hotKey.Control}, Shift={hotKey.Shift}"
                + $", Win={hotKey.Win}, Key={hotKey.Key}");
            Debug.WriteLine($"HotKeyService.UpdateHotKey: 完了");
        }

        /// <summary>
        /// サービスの終了処理（HotKeyの解除）
        /// </summary>
        public void Shutdown()
        {
            UnregisterCurrentHotKey();
            Debug.WriteLine($"HotKeyService.Shutdown: 完了");
        }

        /// <summary>
        /// WndProcでのメッセージ処理
        /// </summary>
        /// <param name="msg">メッセージID</param>
        /// <param name="wParam">wParam</param>
        /// <returns>処理した場合true</returns>
        public bool ProcessWindowMessage(int msg, IntPtr wParam)
        {
            if (msg == WM_HOTKEY && wParam.ToInt32() == _hotKeyId)
            {
                Debug.WriteLine($"HotKeyService.ProcessWindowMessage: HotKey押下検出 ID={wParam.ToInt32()}");
                HotKeyPressed?.Invoke(this, EventArgs.Empty);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 現在のHotKeyを登録
        /// </summary>
        private void RegisterCurrentHotKey(HotKey hotKey)
        {
            if (_window == null)
            {
                throw new InvalidOperationException("ウィンドウが登録されていません。Initialize()を先に呼び出してください。");
            }

            if (_isRegistered)
            {
                UnregisterCurrentHotKey();
            }

            HotKeyHelper.RegisterHotKey(_window, _hotKeyId, hotKey);
            _isRegistered = true;
            Debug.WriteLine($"HotKeyService.RegisterCurrentHotKey: ID={_hotKeyId}");
        }

        /// <summary>
        /// 現在登録されているHotKeyを解除
        /// </summary>
        private void UnregisterCurrentHotKey()
        {
            if (_window != null && _isRegistered)
            {
                HotKeyHelper.UnregisterHotKey(_window, _hotKeyId);
                _isRegistered = false;
                Debug.WriteLine($"HotKeyService.UnregisterCurrentHotKey: ID={_hotKeyId}");
            }
        }

    }
}
