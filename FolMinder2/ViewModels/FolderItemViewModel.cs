using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Text;
using FolMinder2.Models;
using System.Windows.Input;

namespace FolMinder2.ViewModels
{
    public partial class FolderItemViewModel : ObservableObject
    {
        public FolderItem Source { get; }

        [ObservableProperty]
        private bool _pinned;
        [ObservableProperty]
        private string _keyName;
        [ObservableProperty]
        private Key _key;
        [ObservableProperty]
        private string _displayName;

        public FolderItemViewModel(FolderItem source, string keyName, Key key)
        {
            this.Source = source;
            this.Pinned = source.Pinned;
            this.KeyName = keyName;
            this.Key = key;
            this.DisplayName = source.Path;
        }

        partial void OnPinnedChanged(bool oldValue, bool newValue)
        {
            this.Source.Pinned = newValue;
        }
    }
}
