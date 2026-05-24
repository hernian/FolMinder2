using System;
using System.Collections.Generic;
using System.Text;

namespace FolMinder2.Models
{
    public class FolderItem : IComparable<FolderItem>
    {
        public bool Pinned { get; set; }
        public string Path { get; }
        public IReadOnlyList<string> Segments { get; }
        public FolderItem(bool pinned, string path)
        {
            this.Pinned = pinned;
            this.Path = path;
            this.Segments = path.Split(System.IO.Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
        }

        public int CompareTo(FolderItem? other)
        {
            if (other is null)
            {
                return 1;
            }
            var count = Math.Min(this.Segments.Count, other.Segments.Count);
            for (var i = 0; i < count; i++)
            {
                var r = string.Compare(this.Segments[i], other.Segments[i], StringComparison.OrdinalIgnoreCase);
                if (r != 0)
                {
                    return r;
                }
            }
            return this.Segments.Count - other.Segments.Count;
        }
    }
}
