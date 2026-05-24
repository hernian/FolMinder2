using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using FolMinder2.Models;

namespace FolMinder2.Platform
{
    public interface IShellExecuteService
    {
        void OpenFolder(string path);
        void OpenExplorer(string? path = null);
    }

    public class ShellExecuteService : IShellExecuteService
    {
        private const string GUID_PATTERN = @"^::\{[0-9A-Fa-f]{8}-([0-9A-Fa-f]{4}-){3}[0-9A-Fa-f]{12}\}$";
        public const string GUID_PC = "::{20D04FE0-3AEA-1069-A2D8-08002B30309D}";
        public const string GUID_HOME = "::{f0d63f95-3643-4369-ad58-3652136415a0}";

        public ShellExecuteService()
        {
        }

        public void OpenFolder(string path)
        {
            if (!Regex.IsMatch(path, GUID_PATTERN) && !Directory.Exists(path))
            {
                throw new OpenFolderException($"Directory don't exist. Path: {path}", path);
            }
            var psi = new ProcessStartInfo()
            {
                Verb = "Open",
                FileName = path,
                UseShellExecute = true,
            };
            Process.Start(psi);
        }

        public void OpenExplorer(string? path = null)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new OpenFolderException($"Empty path.");
            }
            if (!Regex.IsMatch(path, GUID_PATTERN) && !Directory.Exists(path))
            {
                throw new OpenFolderException($"Directory don't exist. Path: {path}", path);
            }
            var psi = new ProcessStartInfo()
            {
                FileName = "explorer.exe",
                Arguments = path ?? string.Empty,
                UseShellExecute = true,
            };
            Process.Start(psi);
        }
    }
}
