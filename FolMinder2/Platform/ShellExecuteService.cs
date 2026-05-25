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

    public partial class ShellExecuteService : IShellExecuteService
    {
        private const string GUID_PATTERN = @"^::\{[0-9A-Fa-f]{8}-([0-9A-Fa-f]{4}-){3}[0-9A-Fa-f]{12}\}$";

        public ShellExecuteService()
        {
        }

        public void OpenFolder(string path)
        {
            if (!GuidRegex().IsMatch(path) && !Directory.Exists(path))
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
            if (!GuidRegex().IsMatch(path) && !Directory.Exists(path))
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

        [GeneratedRegex(GUID_PATTERN)]
        private static partial Regex GuidRegex();
    }
}
