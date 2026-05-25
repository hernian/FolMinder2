using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;
using System.Xml.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FolMinder2.Infrastructure;
using FolMinder2.Models;
using FolMinder2.Platform;
using FolMinder2.Presentation;
using FolMinder2.Services;
using Serilog;

namespace FolMinder2.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private static IEnumerable<Key> GetKeys()
        {
            for (var key = Key.A; key <= Key.Z; key++)
            {
                yield return key;
            }
            for (var key = Key.D0; key <= Key.D9; key++)
            {
                yield return key;
            }
        }

        public event EventHandler? WindowHideRequired;
        public event EventHandler<DialogRequiredEventArgs>? DialogRequired;

        [ObservableProperty]
        private ObservableCollection<FolderItemViewModel> _items;
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(OpenFolderCommand))]
        private FolderItemViewModel? _selectedItem;

        private readonly IShellFolderService _shellFolderService;
        private readonly IShellExecuteService _shellExecuteService;
        private readonly IHotKeyService _hotKeyService;
        private readonly ISettingsService _settingsService;

        public MainViewModel(
            IShellFolderService shellFolderService,
            IShellExecuteService shellExecuteService,
            IHotKeyService hotKeyService,
            ISettingsService settingsService)
        {
            _shellFolderService = shellFolderService;
            _shellExecuteService = shellExecuteService;
            _hotKeyService = hotKeyService;
            _settingsService = settingsService;

            var pinnedFolders = _settingsService.PinnedFolders.Select(path => new FolderItem(pinned: true, path));
            _shellFolderService.RegisterPinnedFolders(pinnedFolders);

            this.Items = new();
            this.Update();
        }

        public void Initialize()
        {
            CommandHarness(nameof(Initialize), () =>
            {
                var hotKey = _settingsService.HotKey;
                _hotKeyService.UpdateHotKey(hotKey);
            });
        }

        public void Update()
        {
            CommandHarness(nameof(Update), () =>
            {
                this.SelectedItem = null;
                this.Items.Clear();
                // フォルダー一覧とKey生成を結合して短い方だけ周る
                foreach (var (folderItem, key) in _shellFolderService.GetFolderItemList().Zip(GetKeys()))
                {
                    var fivm = new FolderItemViewModel(folderItem, key.ToString(), key);
                    Debug.WriteLine($"Update. fivm: {fivm.Pinned}, {fivm.DisplayName}, {fivm.Source.Path}");
                    this.Items.Add(fivm);
                }
                if (this.Items.Count > 0)
                {
                    this.SelectedItem = this.Items[0];
                }
            });
        }

        public void Config()
        {
            CommandHarness(nameof(Config), () =>
            {
                var configViewModel = new ConfigViewModel(_settingsService);
                var e = new DialogRequiredEventArgs(configViewModel);
                this.DialogRequired?.Invoke(this, e);
                if (e.DialogResult == true)
                {
                    var hotKey = _settingsService.HotKey;
                    _hotKeyService.UpdateHotKey(hotKey);
                }
            });
        }

        public bool OnKey(Key key)
        {
            return CommandHarness(nameof(OpenExplorer), () =>
            {
                if (key == Key.Space)
                {
                    if (this.SelectedItem is not null)
                    {
                        this.SelectedItem.Pinned = !this.SelectedItem.Pinned;
                    }
                    return true;
                }
                var selectedFivm = this.Items.FirstOrDefault(fivm => fivm.Key == key);
                if (selectedFivm is null)
                {
                    return false;
                }
                this.SelectedItem = selectedFivm;
                if (_settingsService.QuickSelect)
                {
                    this.OpenFolder();
                }
                return true;
            }, false);
        }


        public void Shutdown()
        {
            CommandHarness(nameof(Shutdown), () =>
            {
                this.Save();
            });
        }

        [RelayCommand]
        private void OpenExplorer()
        {
            CommandHarness(nameof(OpenExplorer), () =>
            {
                _shellExecuteService.OpenExplorer(ShellExecuteConstants.GUID_PC);
                this.WindowHideRequired?.Invoke(this, EventArgs.Empty);
                this.RegisterPinnedFolders();
            });
        }

        [RelayCommand(CanExecute = nameof(CanOpenFolder))]
        public void OpenFolder()
        {
            CommandHarness(nameof(OpenFolder), () =>
            {
                if (this.SelectedItem is not null)
                {
                    if (_settingsService.PinSelectedFolder)
                    {
                        this.SelectedItem.Pinned = true;
                    }
                    _shellExecuteService.OpenFolder(this.SelectedItem.Source.Path);
                }
                this.WindowHideRequired?.Invoke(this, EventArgs.Empty);
                this.RegisterPinnedFolders();
            });
        }

        private bool CanOpenFolder()
        {
            return this.SelectedItem is not null;
        }

        [RelayCommand]
        private void Close()
        {
            CommandHarness(nameof(Close), () =>
            {
                this.WindowHideRequired?.Invoke(this, EventArgs.Empty);
                this.RegisterPinnedFolders();
            });
        }

        private void RegisterPinnedFolders()
        {
            _shellFolderService.RegisterPinnedFolders(
                this.Items
                .Where(fivm => fivm.Pinned)
                .Select(fivm => fivm.Source));
        }

        private void Save()
        {
            var pinnedFolders = this.Items
                .Where(fivm => fivm.Pinned)
                .Select(fivm => fivm.Source.Path).ToArray()
                ?? new string[] { };
            _settingsService.PinnedFolders = pinnedFolders;
            _settingsService.Save();
        }

        private void CommandHarness(string name, Action action)
        {
            Log.Debug($"{name} enter.");
            try
            {
                action();
            }
            catch (Exception ex)
            {
                HandleException(ex, name);
            }
            finally
            {
                Log.Debug($"{name} leave.");
            }
        }

        private T CommandHarness<T>(string name, Func<T> func, T defaultValue)
        {
            Log.Debug($"{name} enter.");
            try
            {
                return func();
            }
            catch (Exception ex)
            {
                HandleException(ex, name);
            }
            finally
            {
                Log.Debug($"{name} leave.");
            }
            return defaultValue;
        }

        private void HandleException(Exception ex, string name)
        {
            if (ex is OpenFolderException opex)
            {
                Log.Error(ex, $"Exception occured in processing command. Command: {name}");
                ToastMessage.SendError($"フォルダーを開けません。\n{opex.Path}");
            }
            else
            {
                Log.Error(ex, $"Exception occured in processing command. Command: {name}");
                ToastMessage.SendError($"エラーです");
            }
        }
    }
}
