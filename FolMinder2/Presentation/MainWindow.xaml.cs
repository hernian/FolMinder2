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
using FolMinder2.Platform;
using FolMinder2.Presentation;
using FolMinder2.Services;
using FolMinder2.ViewModels;
using Microsoft.Win32;

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
        private bool _initialized = false;
        private bool _isExplicitClose = false;
        private readonly DispatcherTimer _updateWindowSizeTimer;
        private readonly DispatcherTimer _updateDisplayNameTimer;
        private readonly Toast _toast;
        private Task _updateTask = Task.CompletedTask;
        private CancellationTokenSource? _cts;

        public MainWindow(
            MainViewModel viewModel,
            IHotKeyService hotKeyService)
        {
            InitializeComponent();

            _viewModel = viewModel;
            _hotKeyService = hotKeyService;

            this.DataContext = viewModel;
            _viewModel.Items.CollectionChanged += Items_CollectionChanged;
            _viewModel.WindowHideRequired += viewModel_WindowHideRequired;
            _viewModel.DialogRequired += viewModel_DialogRequired;
            _hotKeyService.HotKeyPressed += (_, __) => this.UpdateContents();

            this.SourceInitialized += MainWindow_SourceInitialized;
            this.ContentRendered += MainWindow_ContentRendered;
            this.Loaded += MainWindow_Loaded;
            this.SizeChanged += MainWindow_SizeChanged;
            this.PreviewKeyDown += MainWindow_PreviewKeyDown;
            
            folderListView.Loaded += folderListView_Loaded;
            folderListView.MouseDoubleClick += folderListView_MouseDoubleClick;

            _updateWindowSizeTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(10)
            };
            _updateWindowSizeTimer.Tick += updateWindowSizeTimer_Tick;

            _updateDisplayNameTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)   // ユーザー操作が止まったと判断する時間[ms]
            };
            _updateDisplayNameTimer.Tick += updateDisplayNameTimer_Tick;

            _toast = new Toast(this);

            SystemEvents.SessionEnding += SystemEvents_SessionEnding;
        }

        private void SystemEvents_SessionEnding(object sender, SessionEndingEventArgs e)
        {
            Debug.WriteLine("SystemEvents_SessionEnding");
            _isExplicitClose = true;
            Application.Current.Shutdown();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            Debug.WriteLine($"OnClosing. isExplicitClose: {_isExplicitClose}");

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
        }

        private void updateWindowSizeTimer_Tick(object? sender, EventArgs e)
        {
            _updateWindowSizeTimer.Stop();
            this.UpdateWindowSize();
            Dispatcher.BeginInvoke(() =>
            {
                if (_viewModel.Items.Count > 0)
                {
                    var firstItem = folderListView.ItemContainerGenerator.ContainerFromIndex(0) as ListViewItem;
                    firstItem?.Focus();
                }
            }, DispatcherPriority.Render);
        }

        private void Items_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            Debug.WriteLine("Items_CollectionChanged");
            if (!_initialized)
            {
                return;
            }
            // CollectionChangeが連続発火しても良いようにデバウンス処理する
            _updateWindowSizeTimer.Stop();
            _updateWindowSizeTimer.Start();
        }

        private void folderListView_Loaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("FolderListView_Loaded");
            ListViewHelper.DisableColumnResize(folderListView);
            var workingArea = ScreenHelper.GetWorkingArea(this);
            var left = Math.Max((workingArea.Width - this.Width) / 2, workingArea.Left);
            var top = Math.Max((workingArea.Height - this.Height) / 2, workingArea.Top);
            this.Left = left;
            this.Top = top;
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
            Log.Debug($"MainWindow_SourceInitialized. hWnd: {h:x8}");
            var hwndSource = HwndSource.FromHwnd(h);
            hwndSource.AddHook(WndProc);
            _hotKeyService.Initialize(this, HOTKEY_ID);
            trayIcon.ForceCreate();

            _viewModel.Initialize();
            MoveToCenterOfWorkingArea();
        }

        private void MainWindow_ContentRendered(object? sender, EventArgs e)
        {
            Log.Debug($"MainWindow_ContentRendered. _initialized: {_initialized}");
            if (_initialized)
            {
                return;
            }
            _initialized = true;
            _updateWindowSizeTimer.Stop();
            _updateWindowSizeTimer.Start();
        }

        private void UpdateWindowSize()
        {
            Log.Debug("UpdateWindowSize");
            // 再度 Height を適用
            this.SizeToContent = SizeToContent.Manual;
            this.SizeToContent = SizeToContent.Height;
            // TODO: ここで高さをworkingAreaの高さの0.7倍以下に調整すること
            var workingArea = ScreenHelper.GetWorkingArea(this);
            var widthMax = workingArea.Width * WORKING_AREA_SCALE;
            var width = Math.Min(GetPreferredWindowWidth(), widthMax);
            Debug.WriteLine($"ActualWidth: {this.ActualWidth:F1}, PreferredWindowWidth: {width:F1}");
            this.Width = width;

            Dispatcher.BeginInvoke(() =>
            {
                var left = Math.Max((workingArea.Width - width) / 2, workingArea.Left);
                var top = Math.Max((workingArea.Height - this.Height) / 2, workingArea.Top);
                this.Left = left;
                this.Top = top;
            }, DispatcherPriority.Render);
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Log.Debug("MainWindow_Loaded");
            MoveToCenterOfWorkingArea();
        }


        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Log.Debug("MainWindow_SizeChanged");
            if (!_initialized)
            {
                return;
            }
            _updateDisplayNameTimer.Stop();
            _updateDisplayNameTimer.Start();
            AdjustColNameWidth();
        }
        private async void updateDisplayNameTimer_Tick(object? sender, EventArgs e)
        {
            _updateDisplayNameTimer.Stop();
            StartUpdateDisplayNames();
            try
            {
                await _updateTask;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
            _cts = null;
        }

        private double GetPreferredWindowWidth()
        {
            var lvGap = this.ActualWidth - folderListView.ActualWidth;
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
                // Debug.WriteLine($"GetPreferredWindowWidth. colWidth: {colWidth:F1}");
                maxColWidth = Math.Max(maxColWidth, colWidth);
            }
            var preferredWidth = maxColWidth + lvGap + colGap + colPinned.Width + colKey.Width + SystemParameters.VerticalScrollBarWidth;
            if (preferredWidth > this.MaxWidth)
            {
                preferredWidth = this.MaxWidth;
            }
            return preferredWidth;
        }

        private void AdjustColNameWidth()
        {
            var usedWidgh = colPinned.Width
                + colKey.Width
                + SystemParameters.VerticalScrollBarWidth
                + 8;
            colName.Width = Math.Max(0, folderListView.ActualWidth - usedWidgh);
        }

        private void StartUpdateDisplayNames()
        {
            if (_cts is not null)
            {
                Debug.WriteLine("StartUpdateDisplayNames overlap detected.");
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
        }

        private async Task UpdateDisplayNamesAsync(CancellationToken ct)
        {
            Debug.WriteLine($"UpdateDisplayNamesAsync. colName.ActualWidth: {colName.ActualWidth:F1}");
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
            Debug.WriteLine($"UpdateDisplayNamesAsync. {sw.Elapsed.TotalMilliseconds:F3} ms");
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
            _viewModel.Update();
            if (this.Visibility != Visibility.Visible)
            {
                this.Show();
            }
            WinApiHelper.SetForegroundWindow(this);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            handled = _hotKeyService.ProcessWindowMessage(msg, wParam);
            return IntPtr.Zero;
        }

        private void viewModel_WindowHideRequired(object? sender, EventArgs e)
        {
            Debug.WriteLine("viewModel_WindowHideRequired");
            this.Hide();
        }

        private void Menu_About_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Menu_About_Click");
            menuAbout.IsEnabled = false;
            menuConfig.IsEnabled = false;
            try
            {
                MessageBox.Show(this, "FolMinder2", "FolMinder2", MessageBoxButton.OK, MessageBoxImage.Question);
            }
            finally
            {
                menuConfig.IsEnabled = true;
                menuAbout.IsEnabled = true;
            }
        }
        private void Menu_Open_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Menu_Open_Click");
            Log.Debug($"  MainWindow. Left: {this.Left}, Top: {this.Top}, Width: {this.ActualWidth} Height: {this.ActualHeight}");
            UpdateContents();
        }
        private void Menu_Config_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Menu_Config_Click");
            Log.Debug($"  MainWindow. Left: {this.Left}, Top: {this.Top}, Width: {this.ActualWidth} Height: {this.ActualHeight}");
            menuAbout.IsEnabled = false;
            menuConfig.IsEnabled = false;
            try
            {
                _viewModel.Config();
            }
            finally
            {
                menuConfig.IsEnabled = true;
                menuAbout.IsEnabled = true;
            }
        }

        // メニュー：終了
        private void Menu_Exit_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Menu_Exit_Click");
            _isExplicitClose = true; // 明示的な終了フラグを立てる
            Application.Current.Shutdown();
        }
        private void TrayIcon_LeftMouseDoubleClick(object sender, EventArgs e)
        {
            Log.Debug("TrayIcon_LeftMouseDoubleClick");
            Log.Debug($"  MainWindow. Left: {this.Left}, Top: {this.Top}, Width: {this.ActualWidth} Height: {this.ActualHeight}");
            UpdateContents();
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
            if (_viewModel.OnKey(e.Key))
            {
                e.Handled = true;
            }
        }

        private void MoveToCenterOfWorkingArea()
        {
            Log.Debug($"{nameof(MoveToCenterOfWorkingArea)} enver");
            var workingArea = ScreenHelper.GetWorkingArea(this);
            var left = Math.Max(workingArea.Left + (workingArea.Width - this.ActualWidth) / 2, workingArea.Left);
            var top = Math.Max(workingArea.Top + (workingArea.Height - this.ActualHeight) / 2, workingArea.Top);
            this.Left = left;
            this.Top = top;
            Log.Debug($"  MainWindow. Left: {this.Left}, Top: {this.Top}, Width: {this.ActualWidth} Height: {this.ActualHeight}");
        }
    }
}