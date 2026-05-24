using FolMinder2.Models;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace FolMinder2.Platform
{
    public record FolMinderSettings(
        string[] PinnedFolders,
        bool PinSelectedFolder,
        HotKey HotKey);

    public interface ISettingsStorage
    {
        IReadOnlyList<string> PinnedFolders { get; set; }
        bool PinSelectedFolder { get; set; }
        HotKey HotKey { get; set; }
        void Save();
    }

    public class SettingsStorage : ISettingsStorage
    {
        private readonly TagLog<SettingsStorage> Log = new();

        public IReadOnlyList<string> PinnedFolders { get; set; } = [];
        public bool PinSelectedFolder { get; set; } = true;
        public HotKey HotKey { get; set; } = HotKey.Default;

        private string _settingsJsonPath;

        public SettingsStorage()
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var dir = Path.Combine(localAppData, "Hernian", "FolMinder2");
            _settingsJsonPath = Path.Combine(dir, "settings.json");
            Directory.CreateDirectory(dir);
            try
            {
                var settings = Load();
                foreach (var path in settings.PinnedFolders)
                {
                    Log.Debug($"Loaded PinnedFolder: {path}");
                }
                Log.Debug($"Loaded PinSelectedFoldes: {settings.PinSelectedFolder}");
                Log.Debug($"Loaded HotKey: {settings.HotKey}");
                this.PinnedFolders = settings.PinnedFolders;
                this.PinSelectedFolder = settings.PinSelectedFolder;
                this.HotKey = settings.HotKey;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Loading settings error.");
            }
        }
     
        public void Save()
        {
            try
            {
                foreach (var path in this.PinnedFolders)
                {
                    Log.Debug($"Save. PinnedFolder: {path}");
                }
                Log.Debug($"Save. PinSelectedFolder: {this.PinSelectedFolder}");
                Log.Debug($"Save. HotKey: {this.HotKey}");
                var settings = new FolMinderSettings(
                    this.PinnedFolders.ToArray(),
                    this.PinSelectedFolder,
                    this.HotKey);
                using var stream = new FileStream(_settingsJsonPath, FileMode.Create, FileAccess.Write, FileShare.None);
                JsonSerializer.Serialize(stream, settings);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        private FolMinderSettings Load()
        {
            using var stream = new FileStream(_settingsJsonPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var settings= JsonSerializer.Deserialize<FolMinderSettings>(stream);
            if (settings is null)
            {
                throw new InvalidDataException("Missing stored data.");
            }
            return settings;
        }
    }
}
