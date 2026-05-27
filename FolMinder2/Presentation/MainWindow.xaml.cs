using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using FolMinder2.Infrastructure;
using FolMinder2.Models;
using FolMinder2.Platform;
using FolMinder2.Presentation;
using FolMinder2.Services;
using FolMinder2.ViewModels;
using Microsoft.Win32;
using static FolMinder2.Platform.WinApiHelper;

namespace FolMinder2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // 短縮されないはずの文字列が短縮されないように
        // 必要なWindow幅を求めるときは大き目に、カラムに入るように文字列を短縮するときは小さ目にマージンを取る
        private const int COLUMN_MARGIN_FOR_PREFERRED_SIZE = 32;
        private const int COULMN_MARGIN_FOR_TRUNCATE_NAME = 20;
        // ホットキーはポップアップの1種類なのでなんでも良い
        private const int HOTKEY_ID = 1;
        // ウィンドウの最大サイズを求める
        private const float WORKING_AREA_SCALE = 0.7f;

        private readonly TagLog<MainWindow> Log = new();
        private readonly MainViewModel _viewModel;
        private readonly IHotKeyService _hotKeyService;
        private bool _isExplicitClose = false;
        private readonly Toast _toast;
        private Task _updateTask = Task.CompletedTask;
        private CancellationTokenSource? _cts;
        private readonly IReadOnlyList<MenuItem> _modalMenuItems;

        public MainWindow(
            MainViewModel viewModel,
            IHotKeyService hotKeyService)
        {
            InitializeComponent();

            _viewModel = viewModel;
            _hotKeyService = hotKeyService;

            this.DataContext = viewModel;
            _viewModel.WindowHideRequired += viewModel_WindowHideRequired;
            _viewModel.DialogRequired += viewModel_DialogRequired;
            _hotKeyService.HotKeyPressed += (_, __) => this.UpdateContents();

            this.SourceInitialized += MainWindow_SourceInitialized;
            this.PreviewKeyDown += MainWindow_PreviewKeyDown;
            
            folderListView.MouseDoubleClick += folderListView_MouseDoubleClick;

            _toast = new Toast(this);

            SystemEvents.SessionEnding += (_, __) => this.Shutdown();

            _modalMenuItems = [menuAbout, menuConfig];
        }

        private async void Shutdown()
        {
            Log.Debug("Shutdown enter");
            // _isExplicitCloseがfalseだと、OnClosingでMainWindowを非表示にするだけになってしまう。
            // _isExplicitCloseがtrueだと、普通にMainWindowが閉じるようになる。
            _isExplicitClose = true;
            if (_cts is not null)
            {
                _cts.Cancel();
                try
                {
                    await _updateTask;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Shutdown. While cancelling _updateTask");
                }
                _cts.Dispose();
                _cts = null;
            }
            Application.Current.Shutdown();
            Log.Debug("Shutdown leave");
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            Log.Debug($"OnClosing. isExplicitClose: {_isExplicitClose}");

            if (_isExplicitClose)
            {
                _toast.Dispose();
                _viewModel.Shutdown();
                trayIcon.Dispose();
                _hotKeyService.Shutdown();
            }
            else
            {
                e.Cancel = true;
                this.Hide();
            }
            base.OnClosing(e);
            Log.Debug("OnClosing leave");
        }


        private void folderListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var item = (e.OriginalSource as FrameworkElement)?.DataContext;
            if (item == null || item == CollectionView.NewItemPlaceholder)
                return;
            if (folderListView.SelectedItem is not null)
            {
                _viewModel.OpenFolder();
            }
        }

        private void MainWindow_SourceInitialized(object? sender, EventArgs e)
        {
            var helper = new WindowInteropHelper(this);
            var h = helper.Handle;
            Log.Debug($"MainWindow_SourceInitialized enter. hWnd: {h:x8}");
            var hwndSource = HwndSource.FromHwnd(h);
            hwndSource.AddHook(WndProc);
            _hotKeyService.Initialize(this, HOTKEY_ID);
            trayIcon.ForceCreate();
            _viewModel.Initialize();
            Log.Debug($"MainWindow_SourceInitialized. leave");
        }

        private double GetPreferredWindowWidth()
        {
            Log.Debug("GetPreferredWindowWidth enter");
            var lvGap = this.ActualWidth - folderListView.ActualWidth;
            Log.Debug($"lvGap: {lvGap:F1}, ActualWidth: {this.ActualWidth:F1}, folderListView.ActualWidth: {folderListView.ActualWidth:F1}");
            // カラム内最大幅の文字列と実際のカラム幅のギャップ
            // 計算で求めるのが難しいので、定数でごまかす
            var colGap = COLUMN_MARGIN_FOR_PREFERRED_SIZE;

            // Debug.WriteLine($"GetPreferredWindowWidth. ActualWidth: {this.ActualWidth:F1}, colName.ActualWidth: {colName.ActualWidth:F1}, colGap: {colGap:F1}");
            var maxColWidth = 0.0;
            var truncatedNameBuilder = CreateTruncatedNameBuilder();
            var items = (ObservableCollection<FolderItemViewModel>)folderListView.ItemsSource;
            foreach (var fivm in items)
            {
                var colWidth = truncatedNameBuilder.MeasureTextWidth(fivm.Source.Path);
                // Log.Debug($"GetPreferredWindowWidth. colWidth: {colWidth:F1}, Path: {fivm.Source.Path}");
                maxColWidth = Math.Max(maxColWidth, colWidth);
            }
            Log.Debug($"maxColWidth: {maxColWidth:F1}");
            var preferredWidth = maxColWidth + lvGap + colGap + colPinned.Width + colKey.Width + SystemParameters.VerticalScrollBarWidth;
            if (preferredWidth > this.MaxWidth)
            {
                preferredWidth = this.MaxWidth;
            }
            Log.Debug($"GetPreferredWindowWidth leave. preferredWidth: {preferredWidth:F1}");
            return preferredWidth;
        }

        private void AdjustColNameWidth()
        {
            Log.Debug("AdjustColNameWidth enter");
            var usedWidgh = colPinned.Width
                + colKey.Width
                + SystemParameters.VerticalScrollBarWidth
                + 8;
            colName.Width = Math.Max(0, folderListView.ActualWidth - usedWidgh);
            Log.Debug("AdjustColNameWidth leave");
        }

        private void StartUpdateDisplayNames()
        {
            Log.Debug("StartUpdateDisplayNames enter");
            if (_cts is not null)
            {
                Log.Debug("StartUpdateDisplayNames overlap detected.");
                _cts.Cancel();
                try
                {
                    _updateTask.Wait();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }
                _cts = null;
            }
            _cts = new CancellationTokenSource();
            _updateTask = UpdateDisplayNamesAsync(_cts.Token);
            Log.Debug("StartUpdateDisplayNames leave");
        }

        private async Task UpdateDisplayNamesAsync(CancellationToken ct)
        {
            Log.Debug($"UpdateDisplayNamesAsync enter. colName.ActualWidth: {colName.ActualWidth:F1}");
            var items = (ObservableCollection<FolderItemViewModel>?)folderListView.ItemsSource;
            if (items == null)
            {
                return;
            }
            var sw = Stopwatch.StartNew();
            // 非同期ループの処理中に folderListView.ItemsSource の参照先が変更される可能性がある
            // そのため、配列にコピーしてからループする
            var itemArray = items.ToArray();
            var truncatedNameBuilder = CreateTruncatedNameBuilder();
            foreach (var fivm in itemArray)
            {
                var segments = fivm.Source.Segments;
                var path = fivm.Source.Path;
                var displayName = await Task.Run(() => truncatedNameBuilder.Build(segments, path), ct);
                if (fivm.DisplayName != displayName)
                {
                    fivm.DisplayName = displayName;
                }
            }
            sw.Stop();
            Log.Debug($"UpdateDisplayNamesAsync leave. {sw.Elapsed.TotalMilliseconds:F3} ms");
        }

        private TruncatedNameBuilder CreateTruncatedNameBuilder()
        {
            double pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
            double maxWidth = colName.ActualWidth - COULMN_MARGIN_FOR_TRUNCATE_NAME;
            var truncatedNameBuilder = new TruncatedNameBuilder(maxWidth, folderListView, pixelsPerDip); // 20は適当なマージン
            return truncatedNameBuilder;
        }

        private void UpdateContents()
        {
            Log.Debug($"UpdateContents enter. _isExplicitClose: {_isExplicitClose}");
            if (_isExplicitClose)
            {
                Log.Debug("UpdateContents leave");
                return;
            }

            Log.Debug("Set _isActivated to true.");
            _viewModel.Update();

            var workingArea = ScreenHelper.GetWorkingArea(this);
            var maxWidth = workingArea.Width * WORKING_AREA_SCALE;
            var maxHeight = workingArea.Height * WORKING_AREA_SCALE;

            {
                // this.Show()を呼ばないとMainWindow上のコントロール群が生成されない。
                // コントロールのうちListViewが生成されないと丁度良いMainWindowのサイズが分からない。
                // なので、一度、見えないところへ移動してから表示する
                var left = -32000;
                var top = -32000;
                var swp = new SetWindowPosParam(
                    ChangePosition: true,
                    Left: left,
                    Top: top);
                WinApiHelper.SetWindowPos(this, swp);
            }
            this.WindowState = WindowState.Normal;
            this.Show();
            {
                var preferredWidth = GetPreferredWindowWidth();
                var candWidth = Math.Min(preferredWidth, maxWidth);
                // MainWindowの高さの自動計算を強制的に適用する。
                // SizeToContent を Height(XAMLで指定) → Manual →Height と変化させるとWindowの高さが再設定される。
                this.SizeToContent = SizeToContent.Manual; 
                this.SizeToContent = SizeToContent.Height;
                var preferredHeight = this.ActualHeight;
                var candHeight = Math.Min(preferredHeight, maxHeight);
                var left = Math.Max(workingArea.Left + (workingArea.Width - candWidth) / 2, workingArea.Left);
                var top = Math.Max(workingArea.Top + (workingArea.Height - candHeight) / 2, workingArea.Left);
                Log.Debug($"  SetWindowPos to FitSize. Left: {left:F1}, Top: {top:F1}, Width: {candWidth:F1}, Height: {candHeight:F1}");
                var swp = new SetWindowPosParam(
                    ChangePosition: true,
                    ChangeSize: true,
                    Left: left,
                    Top: top,
                    Width: candWidth,
                    Height: candHeight);
                WinApiHelper.SetWindowPos(this, swp);
            }
            ListViewHelper.DisableColumnResize(folderListView);
            AdjustColNameWidth();
            if (folderListView.Items.Count > 0)
            {
                folderListView.SelectedIndex = 0;
                var firstItem = (ListViewItem)folderListView.ItemContainerGenerator.ContainerFromIndex(0);
                firstItem.Focus();
            }
            StartUpdateDisplayNames();

            WinApiHelper.SetForegroundWindow(this);
            Log.Debug("UpdateContents leave");
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            handled = _hotKeyService.ProcessWindowMessage(msg, wParam);
            return IntPtr.Zero;
        }

        private async void viewModel_WindowHideRequired(object? sender, EventArgs e)
        {
            Log.Debug("viewModel_WindowHideRequired enter");
            this.Hide();

            await Task.Yield();

            // 非表示のMainWindowの位置・サイズはWindows APIを使った方が確実 by Copilot
            var workingArea = ScreenHelper.GetWorkingArea(this);
            var left = Math.Max(workingArea.Left + (workingArea.Width - this.MinWidth) / 2, workingArea.Left);
            var top = Math.Max(workingArea.Top + (workingArea.Height - this.MinHeight) / 2, workingArea.Top);
            var swp = new SetWindowPosParam(
                ChangeSize: true,
                ChangePosition: true,
                Left: left,
                Top: top,
                Width: this.MinWidth,
                Height: this.MinHeight);
            WinApiHelper.SetWindowPos(this, swp);
            var rect = WinApiHelper.GetWindowRect(this);
            Log.Debug($"  GetWindowRect. Left: {rect.Left}, Top: {rect.Top}, Width: {rect.Width}, Height: {rect.Height}");
            Log.Debug("viewModel_WindowHideRequired leave");
        }

        private void Menu_About_Click(object sender, RoutedEventArgs e)
        {
            Log.Debug("Menu_About_Click enter");
            if (_isExplicitClose)
            {
                return;
            }
            EnableModalMenuItem(false);
            try
            {
                MessageBox.Show(this, "FolMinder2", "FolMinder2", MessageBoxButton.OK, MessageBoxImage.Question);
            }
            finally
            {
                EnableModalMenuItem(true);
            }
            Log.Debug("Menu_About_Click leave");
        }
        private void Menu_Open_Click(object sender, RoutedEventArgs e)
        {
            Log.Debug("Menu_Open_Click enter");
            if (_isExplicitClose)
            {
                return;
            }
            var rect = WinApiHelper.GetWindowRect(this);
            Log.Debug($"  GetWindowRect. Left: {rect.Left}, Top: {rect.Top}, Width: {rect.Width} Height: {rect.Height}");
            Log.Debug($"  MainWindow. Left: {this.Left}, Top: {this.Top}, ActualWidth: {this.ActualWidth} ActualHeight: {this.ActualHeight}");
            UpdateContents();
            Log.Debug("Menu_Open_Click leave");
        }
        private void Menu_Config_Click(object sender, RoutedEventArgs e)
        {
            Log.Debug("Menu_Config_Click enter");
            if (_isExplicitClose)
            {
                return;
            }
            EnableModalMenuItem(false);
            try
            {
                _viewModel.Config();
            }
            finally
            {
                EnableModalMenuItem(true);
            }
            Log.Debug("Menu_Config_Click leave");
        }

        // メニュー：終了
        private void Menu_Exit_Click(object sender, RoutedEventArgs e)
        {
            Log.Debug("Menu_Exit_Click enter");
            if (_isExplicitClose)
            {
                return;
            }
            trayIcon.ContextMenu.IsEnabled = false;
            this.Shutdown();
            Log.Debug("Menu_Exit_Click leave");
        }
        private void TrayIcon_LeftMouseDoubleClick(object sender, EventArgs e)
        {
            Log.Debug("TrayIcon_LeftMouseDoubleClick enter");
            if (!_isExplicitClose)
            {
                var rect = WinApiHelper.GetWindowRect(this);
                Log.Debug($"  GetWindowRect. Left: {rect.Left}, Top: {rect.Top}, Width: {rect.Width} Height: {rect.Height}");
                Log.Debug($"  MainWindow. Left: {this.Left}, Top: {this.Top}, ActualWidth: {this.ActualWidth} ActualHeight: {this.ActualHeight}");
                UpdateContents();
            }
            Log.Debug("TrayIcon_LeftMouseDoubleClick leave");
        }

        private void viewModel_DialogRequired(object? sender, DialogRequiredEventArgs e)
        {
            if (e.ViewModel is ConfigViewModel configViewModel)
            {
                var configDialg = new ConfigDialog(configViewModel)
                {
                    Owner = this
                };
                configDialg.ShowDialog();
                e.DialogResult = configDialg.DialogResult;
                return;
            }
            throw new NotImplementedException();
        }

        private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var key = e.Key;
            if (key == Key.Space)
            {
                // スペースキーが押されたときの動作
                // 1. 選択された項目がないならフォーカス行を選択された状態にする
                // 2. 選択された項目のPin留め状態をトグルする
                if (folderListView.SelectFocusedListViewItem())
                {
                    e.Handled = true;
                }
                else if (folderListView.SelectedItem is FolderItemViewModel fivm)
                {
                    fivm.Pinned = !fivm.Pinned;
                    e.Handled = true;
                }
                return;
            }
            var selectedFivm = folderListView.Items.Cast<FolderItemViewModel>().FirstOrDefault(fivm=> fivm.Key == key);
            if (selectedFivm is not null)
            {
                folderListView.SelectedItem = selectedFivm;
                folderListView.SetFocus(selectedFivm);
                _viewModel.OnFolderSelectedByKey();
                e.Handled = true;
                return;
            }
        }

        private void EnableModalMenuItem(bool enabled)
        {
            foreach (var item in _modalMenuItems)
            {
                item.IsEnabled = enabled;
            }
        }
    }
}