using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Input;
using FolMinder2.Models;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Navigation;
using FolMinder2.Infrastructure;

namespace FolMinder2.ViewModels
{
    public partial class ConfigViewModel : ObservableObject
    {
        private static ObservableCollection<NameKey> CreateNameKeys()
        {
            var collection = new ObservableCollection<NameKey>();
            for (var key = Key.A; key <= Key.Z; key++)
            {
                var nameKey = new NameKey(key.ToString(), key);
                collection.Add(nameKey);
            }
            for (var key = Key.D0; key <= Key.D9; key++)
            {
                var name = ((int)(key - Key.D0)).ToString();
                var nameKey = new NameKey(name, key);
                collection.Add(nameKey);
            }
            collection.Add(new NameKey("変換", Key.ImeConvert));
            collection.Add(new NameKey("無変換", Key.ImeNonConvert));
            return collection;
        }

        public event EventHandler? AcceptRequired;
        public event EventHandler? CancelRequired;

        [ObservableProperty]
        private bool _pinSelectedFolder;
        [ObservableProperty]
        private bool _quickSelect;
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AcceptCommand))]
        private bool _withAlt;
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AcceptCommand))]
        private bool _withControl;
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AcceptCommand))]
        private bool _withShift;
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AcceptCommand))]
        private bool _withWin;

        [ObservableProperty]
        private ObservableCollection<NameKey> _items;
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AcceptCommand))]
        private NameKey? _selectedItem;

        private ISettingsService _settingsStorage;

        public ConfigViewModel(ISettingsService settingsService)
        {
            _settingsStorage = settingsService;
            this.PinSelectedFolder = _settingsStorage.Settings.PinSelectedFolder;
            this.QuickSelect = _settingsStorage.Settings.QuickSelect;
            var hotKey = settingsService.Settings.HotKey;
            this.WithAlt = hotKey.Alt;
            this.WithControl = hotKey.Control;
            this.WithShift = hotKey.Shift;
            this.WithWin = hotKey.Win;
            this.Items = CreateNameKeys();
            this.SelectedItem = _items.FirstOrDefault(nk => nk.Key == hotKey.Key);
        }

        [RelayCommand(CanExecute = nameof(CanAccept))]
        private void Accept()
        {
            _settingsStorage.Settings.PinSelectedFolder = this.PinSelectedFolder;
            _settingsStorage.Settings.QuickSelect = this.QuickSelect;
            var hotKey = new HotKey(
                Alt: this.WithAlt,
                Control: this.WithControl,
                Shift: this.WithShift,
                Win: this.WithWin,
                Key: this.SelectedItem!.Key);
            _settingsStorage.Settings.HotKey = hotKey;
            _settingsStorage.Save();
            this.AcceptRequired?.Invoke(this, EventArgs.Empty);
        }
        private bool CanAccept()
        {
            if (this.SelectedItem is null)
            {
                return false;
            }
            if (!this.WithAlt && !this.WithControl && !this.WithShift && !this.WithWin)
            {
                return false;
            }
            return true;
        }

        [RelayCommand]
        private void Cancel()
        {
            this.CancelRequired?.Invoke(this, EventArgs.Empty);
        }
    }
}
