using System;
using System.Collections.Generic;
using System.Text;
using Serilog;

namespace FolMinder2.Infrastructure
{
    public class TagLog<T> where T : class
    {
        private readonly string _tag;
        public TagLog()
        {
            _tag = $"[{typeof(T).Name}]";
        }

        public void Debug(string msg)
        {
            Log.Debug(_tag + msg);
        }
        public void Error(string msg)
        {
            Log.Error(_tag + msg);
        }
        public void Error(Exception ex, string msg)
        {
            Log.Error(ex, _tag + msg);
        }

        public void Information(string msg)
        {
            Log.Information(_tag + msg);
        }
    }
}
