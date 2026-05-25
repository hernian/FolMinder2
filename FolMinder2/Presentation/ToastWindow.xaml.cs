using System;
using System.Collections.Generic;
using System.Text;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using FolMinder2.Infrastructure;

namespace FolMinder2.Presentation
{
    /// <summary>
    /// ToastWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class ToastWindow : Window
    {
        private static class ResourceKeys
        {
            public const string InfoDuration = "infoDuration";
            public const string InfoBackground = "infoBackgroundBrush";
            public const string InfoForeground = "infoForegroundBrush";

            public const string ErrorDuration = "errorDuration";
            public const string ErrorBackground = "errorBackgroundBrush";
            public const string ErrorForeground = "errorForegroundBrush";
        }

        private readonly DispatcherTimer _timer;

        public ToastWindow(ToastMessage message)
        {
            InitializeComponent();

            var bgKey = message.Type == ToastType.Error ? ResourceKeys.ErrorBackground : ResourceKeys.InfoBackground;
            var fgKey = message.Type == ToastType.Error ? ResourceKeys.ErrorForeground : ResourceKeys.InfoForeground;
            toastBorder.Background = (Brush)FindResource(bgKey);
            messageText.Foreground = (Brush)FindResource(fgKey);
            closeButton.Foreground = (Brush)FindResource(fgKey);

            messageText.Text = message.Text;

            var durationKey = message.Type == ToastType.Error ? ResourceKeys.ErrorDuration : ResourceKeys.InfoDuration;
            var duration = (TimeSpan)FindResource(durationKey);

            closeButton.Click += (_, __) => CloseToast();
            _timer = new DispatcherTimer { Interval = duration };
            _timer.Tick += (_, __) => CloseToast();

            this.Loaded += (_, __) => _timer.Start();
        }
        private void CloseToast()
        {
            _timer?.Stop();
            this.Close();
        }
    }
}
