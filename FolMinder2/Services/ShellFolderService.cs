using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using SHDocVw;
using FolMinder2.Models;
using System.Windows.Controls;
using FolMinder2.Platform;

namespace FolMinder2.Services
{
    public interface IShellFolderService
    {
        void RegisterPinnedFolders(IEnumerable<FolderItem> pinnedFolders);
        IEnumerable<FolderItem> GetFolderItemList();
    }

    public class ShellFolderService : IShellFolderService
    {
        private record WndPath(IntPtr HWnd, string Path);

        private readonly TagLog<ShellFolderService> Log = new();


        private List<FolderItem> _folderItemList = new();

        public ShellFolderService()
        {
            var sw = Stopwatch.StartNew();
            var _ = new ShellWindows();
            sw.Stop();
            Log.Debug($"ShellFolderService. ctor elapsed: {sw.Elapsed.TotalMilliseconds} ms");
        }

        public void RegisterPinnedFolders(IEnumerable<FolderItem> pinnedFolders)
        {
            _folderItemList.Clear();
            foreach (var folder in pinnedFolders)
            {
                if (folder.Pinned)
                {
                    Log.Debug($"SetPinnedFolders: Pinned: {folder.Pinned}, Path: {folder.Path}");
                    _folderItemList.Add(folder);
                }
            }
        }

        public IEnumerable<FolderItem> GetFolderItemList()
        {
            var pathSet = new HashSet<string>();
            var prevList = _folderItemList;
            _folderItemList = new();
            foreach (var fi in prevList)
            {
                if (!fi.Pinned)
                {
                    continue;
                }
                Log.Debug($"GetFolderItemList. Pinned: {fi.Pinned} Path: {fi.Path}");
                _folderItemList.Add(fi);
                pathSet.Add(fi.Path);
            }
            foreach (var wp in GetWndPaths())
            {
                if (!pathSet.Add(wp.Path))
                {
                    continue;
                }
                var fi = new FolderItem(false, wp.Path);
                Log.Debug($"GetFolderItemList. Pinned: {fi.Pinned} Path: {fi.Path}");
                _folderItemList.Add(fi);
            }
            _folderItemList.Sort();
            return _folderItemList;
        }

        private IReadOnlyList<WndPath> GetWndPaths()
        {
            var sw = Stopwatch.StartNew();
            var wndPathList = new List<WndPath>();
            var shellWindows = new ShellWindows();
            Debug.WriteLine($"GetWndPaths. shellWindows.Count: {shellWindows.Count}");
            for (int i = 0; i < shellWindows.Count; i++)
            {
                try
                {
                    var win = shellWindows.Item(i);
                    if (win == null || win!.Document == null)
                    {
                        Log.Debug($"win: {win}, win.Document: {win!.Document}");
                        continue;
                    }
                    string path = win!.Document.Folder.Self.Path;
                    if (path.StartsWith("::{"))
                    {
                        continue;
                    }
                    wndPathList.Add(new WndPath((IntPtr)win.HWND, path));
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "GetWndPaths");
                }
            }
            sw.Stop();
            Log.Debug($"GetWndPaths elapsed: {sw.Elapsed.TotalMilliseconds} ms");
            return wndPathList;
        }
    }
}
