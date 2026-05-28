using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Windows;
using System.Windows.Interop;
using FolMinder2.Platform;
using FolMinder2.Services;
using FolMinder2.ViewModels;
using H.NotifyIcon;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System.IO;
using FolMinder2.Infrastructure;

namespace FolMinder2
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private const string MUTEX_NAME = "Hernian.FolMinder2.Singleton";

        private static ServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();
            services.AddSingleton<ISettingsService, SettingsService>();
            services.AddSingleton<IShellExecuteService, ShellExecuteService>();
            services.AddSingleton<IHotKeyService, HotKeyService>();
            services.AddSingleton<IShellFolderService, ShellFolderService>();
            services.AddSingleton<MainViewModel>();
            services.AddSingleton<MainWindow>();

            return services.BuildServiceProvider();
        }

        private static void InitializeLog()
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var logPath = Path.Combine(localAppData, "Hernian", "FolMinder2", "log", "log-.log");
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(
                    path: logPath,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.Debug()
                .CreateLogger();
        }

        private static Mutex? _mutex;

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            _mutex = new Mutex(true, MUTEX_NAME, out bool createdNew);
            if (!createdNew)
            {
                Shutdown();
                return;
            }

            Log.Information("FolMinder2 started.");
            InitializeLog();
            var provider = ConfigureServices();
            var mainViewModel = provider.GetRequiredService<MainViewModel>();
            var mainWindow = provider.GetRequiredService<MainWindow>();
            this.MainWindow = mainWindow;
            // 非表示のままウィンドウを生成する
            var helper = new WindowInteropHelper(mainWindow);
            helper.EnsureHandle();
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            _mutex?.ReleaseMutex();
            _mutex?.Dispose();
        }
    }

}
