using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using FolMinder2.Platform;
using System.Windows.Input;
using FolMinder2.Models;
using CommunityToolkit.Mvvm.Input;

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
        private bool _withAlt;
        [ObservableProperty]
        private bool _withControl;
        [ObservableProperty]
        private bool _withShift;
        [ObservableProperty]
        private bool _withWin;

        [ObservableProperty]
        private ObservableCollection<NameKey> _items;
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AcceptCommand))]
        private NameKey? _selectedItem;

        private ISettingsStorage _settingsStorage;

        public ConfigViewModel(ISettingsStorage settingsStorage)
        {
            _settingsStorage = settingsStorage;
            var hotKey = settingsStorage.HotKey;
            _withAlt = hotKey.Alt;
            _withControl = hotKey.Control;
            _withShift = hotKey.Shift;
            _withWin = hotKey.Win;
            _items = CreateNameKeys();
            _selectedItem = _items.FirstOrDefault(nk => nk.Key == hotKey.Key);
        }

        [RelayCommand(CanExecute = nameof(CanAccept))]
        private void Accept()
        {
            var hotKey = new HotKey(
                Alt: this.WithAlt,
                Control: this.WithControl,
                Shift: this.WithShift,
                Win: this.WithWin,
                Key: this.SelectedItem!.Key);
            _settingsStorage.HotKey = hotKey;
            _settingsStorage.Save();
            this.AcceptRequired?.Invoke(this, EventArgs.Empty);
        }
        private bool CanAccept()
        {
            return this.SelectedItem is not null;
        }

        [RelayCommand]
        private void Cancel()
        {
            this.CancelRequired?.Invoke(this, EventArgs.Empty);
        }
    }
}
