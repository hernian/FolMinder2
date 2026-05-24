using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FolMinder2.ViewModels;

namespace FolMinder2.Presentation
{
    public class TruncatedNameBuilder
    {
        private const string ELLIPSIS = "…";
        private readonly static char PATH_SEPALATOR = System.IO.Path.DirectorySeparatorChar;

        private readonly CultureInfo _cultureInfo = CultureInfo.CurrentCulture;
        private readonly FlowDirection _flowDirection = FlowDirection.LeftToRight;
        private readonly Brush _brush = Brushes.Black;
        private readonly double _maxWidth;
        private readonly Typeface _typeface;
        private readonly double _fontSize;
        private readonly double _pixelsPerDip;
        private readonly StringBuilder _sb;

        public TruncatedNameBuilder(double maxWidth, ListView listView, double pixelsPerDip)
        {
            _maxWidth = maxWidth;
            _typeface = new Typeface(
                listView.FontFamily,
                listView.FontStyle,
                listView.FontWeight,
                listView.FontStretch);
            _fontSize = listView.FontSize;
            _pixelsPerDip = pixelsPerDip;
            _sb = new StringBuilder();
        }

        public string Build(IReadOnlyList<string> segments, string srcPath)
        {
            if (segments.Count < 3)
            {
                return srcPath;
            }
            string candPath = string.Empty;
            foreach ((var lo, var hi) in GetEllipseRanges(1, segments.Count - 1))
            {
                candPath = this.BuildPath(segments, lo, hi);
                var candWidth = this.MeasureTextWidth(candPath);
                // Debug.WriteLine($"candWidth: {candWidth:F1}, candPath: {candPath}");
                if (candWidth <= _maxWidth)
                {
                    break;
                }
            }
            return candPath;
        }
        public double MeasureTextWidth(string text)
        {
            var ft = new FormattedText(
                text,
                _cultureInfo,
                _flowDirection,
                _typeface,
                _fontSize,
                _brush,
                _pixelsPerDip);
            return ft.Width;
        }

        private IEnumerable<(int Lo, int Hi)> GetEllipseRanges(int start, int end)
        {
            int center = (start + end) / 2;
            int lo = center;
            int hi = center;
            yield return (lo, hi);
            hi++;
            while (lo > start || hi < end)
            {
                if (hi < end)
                    yield return (lo, hi++);
                if (lo > start)
                    yield return (--lo, hi);
            }
        }

        private string BuildPath(IReadOnlyList<string> segments, int lo, int hi)
        {
            _sb.Clear();
            for (var i = 0; i < lo; i++)
            {
                _sb.Append(segments[i]);
                _sb.Append(PATH_SEPALATOR);
            }
            if (lo < hi)
            {
                _sb.Append(ELLIPSIS);
                _sb.Append(PATH_SEPALATOR);
            }
            for (var i = hi; i + 1 < segments.Count; i++)
            {
                _sb.Append(segments[i]);
                _sb.Append(PATH_SEPALATOR);
            }
            _sb.Append(segments[^1]);
            return _sb.ToString();
        }

    }
}
