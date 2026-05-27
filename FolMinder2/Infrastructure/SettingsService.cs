using FolMinder2.Models;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace FolMinder2.Infrastructure
{
    public class FolMinderSettings
    {
        public string[] PinnedFolders { get; set; } = [];
        public bool PinSelectedFolder { get; set; }
        public bool QuickSelect { get; set; }
        public HotKey HotKey { get; set; } = HotKey.Default;
    }

    public interface ISettingsService
    {
        FolMinderSettings Settings { get; }
        void Save();
    }

    public class SettingsService : ISettingsService
    {
        private readonly TagLog<SettingsService> Log = new();
      

        public FolMinderSettings Settings { get; }

        private string _settingsJsonPath;

        public SettingsService()
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var dir = Path.Combine(localAppData, "Hernian", "FolMinder2");
            _settingsJsonPath = Path.Combine(dir, "settings.json");
            Directory.CreateDirectory(dir);
            this.Settings = Load();
            foreach (var path in this.Settings.PinnedFolders)
            {
                Log.Debug($"Loaded PinnedFolder: {path}");
            }
            Log.Debug($"Loaded PinSelectedFoldes: {this.Settings.PinSelectedFolder}");
            Log.Debug($"Loaded QuickSelect: {this.Settings.QuickSelect}");
            Log.Debug($"Loaded HotKey: {this.Settings.HotKey}");
        }
     
        public void Save()
        {
            try
            {
                foreach (var path in this.Settings.PinnedFolders)
                {
                    Log.Debug($"Save. PinnedFolder: {path}");
                }
                Log.Debug($"Save. PinSelectedFolder: {this.Settings.PinSelectedFolder}");
                Log.Debug($"Save. QuickSelect: {this.Settings.QuickSelect}");
                Log.Debug($"Save. HotKey: {this.Settings.HotKey}");
                using var stream = new FileStream(_settingsJsonPath, FileMode.Create, FileAccess.Write, FileShare.None);
                JsonSerializer.Serialize(stream, this.Settings);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        private FolMinderSettings Load()
        {
            try
            {
                using var stream = new FileStream(_settingsJsonPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                var settings = JsonSerializer.Deserialize<FolMinderSettings>(stream)
                    ?? throw new InvalidDataException("Missing stored data.");
                return settings;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Loading stored settings.");
            }
            return new FolMinderSettings();
        }
    }
}
