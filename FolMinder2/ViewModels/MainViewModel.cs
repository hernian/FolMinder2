using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FolMinder2.Platform;
using FolMinder2.Presentation;
using FolMinder2.Services;
using FolMinder2.Models;

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
            var hotKey = _settingsService.HotKey;
            _hotKeyService.UpdateHotKey(hotKey);
        }

        public void Update()
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
        }

        public void Config()
        {
            var configViewModel = new ConfigViewModel(_settingsService);
            var e = new DialogRequiredEventArgs(configViewModel);
            this.DialogRequired?.Invoke(this, e);
            if (e.DialogResult == true)
            {
                var hotKey = _settingsService.HotKey;
                _hotKeyService.UpdateHotKey(hotKey);
            }
        }

        public bool OnKey(Key key)
        {
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
        }

        public void Shutdown()
        {
            this.Save();   
        }

        [RelayCommand]
        private void OpenExplorer()
        {
            Debug.WriteLine("OpenExplorer");
            _shellExecuteService.OpenExplorer();
            this.WindowHideRequired?.Invoke(this, EventArgs.Empty);
        }

        [RelayCommand(CanExecute = nameof(CanOpenFolder))]
        public void OpenFolder()
        {
            Debug.WriteLine("OpenFolder");
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
        }

        private bool CanOpenFolder()
        {
            return this.SelectedItem is not null;
        }

        [RelayCommand]
        private void Close()
        {
            Debug.WriteLine("Close");
            this.WindowHideRequired?.Invoke(this, EventArgs.Empty);
            this.RegisterPinnedFolders();
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
    }
}
