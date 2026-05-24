using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FolMinder2.Models
{
    public class OpenFolderException : DirectoryNotFoundException
    {
        public string? Path { get; init; }

        public OpenFolderException(string message)
            : base(message)
        {
        }

        public OpenFolderException(string message, string? path)
            : base(message)
        {
            this.Path = path;
        }
    }
}
