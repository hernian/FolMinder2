using System.Windows;
using System.Windows.Media;

namespace FolMinder2.Presentation
{
    public static class VisualTreeHelpers
    {
        /// <summary>
        /// ビジュアルツリーから指定された型の子要素を再帰的に検索
        /// </summary>
        /// <typeparam name="T">検索する要素の型</typeparam>
        /// <param name="parent">検索を開始する親要素</param>
        /// <returns>見つかった要素、または null</returns>
        public static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null)
                return null;

            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is T t)
                    return t;

                var result = FindVisualChild<T>(child);
                if (result != null)
                    return result;
            }
            return default;
        }

        /// <summary>
        /// ビジュアルツリーから指定された型の親要素を再帰的に検索
        /// </summary>
        /// <typeparam name="T">検索する要素の型</typeparam>
        /// <param name="child">検索を開始する子要素</param>
        /// <returns>見つかった要素、または null</returns>
        public static T? FindVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            if (child == null)
                return null;

            var parent = VisualTreeHelper.GetParent(child);
            if (parent == null)
                return null;

            if (parent is T t)
                return t;

            return FindVisualParent<T>(parent);
        }
    }
}
