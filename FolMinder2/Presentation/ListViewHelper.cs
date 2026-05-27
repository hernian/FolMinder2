using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace FolMinder2.Presentation
{
    public static class ListViewHelper
    {
        public static TextBlock? GetTextBlockFromColumn(this ListView listView, GridViewColumn column)
        {
            var presenter = FindVisualChild<GridViewHeaderRowPresenter>(listView);
            if (presenter == null) return null;

            int count = VisualTreeHelper.GetChildrenCount(presenter);
            for (int i = 0; i < count; i++)
            {
                if (VisualTreeHelper.GetChild(presenter, i) is GridViewColumnHeader header
                    && header.Column == column)          // ← Column プロパティで照合
                {
                    return FindVisualChild<TextBlock>(header);
                }
            }
            return null;
        }

        public static void DisableColumnResize(DependencyObject? parent)
        {
            if (parent is null)
            {
                return;
            }
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is Thumb thumb && thumb.Name == "PART_HeaderGripper")
                {
                    thumb.IsEnabled = false;
                    thumb.Visibility = Visibility.Collapsed; // カーソル変化も防ぐ
                }
                DisableColumnResize(child);
            }
        }
        public static void SetFocus(this ListView listView, object item)
        {
            var listViewItem = (ListViewItem)listView.ItemContainerGenerator.ContainerFromItem(item);
            listViewItem.Focus();

        }

        public static ListViewItem? GetFocusedListViewItem(this ListView listView)
        {
            var focused = Keyboard.FocusedElement as DependencyObject;
            if (focused is null)
            {
                return null;
            }
            var item = FindAncestor<ListViewItem>(focused);
            if (item is null || !item.IsDescendantOf(listView))
            {
                return null;
            }
            return item;
        }

        public static bool SelectFocusedListViewItem(this ListView listView)
        {
            if (listView.SelectedItem is not null)
            {
                return false;
            }
            var focused = listView.GetFocusedListViewItem();
            if (focused is null)
            {
                return false;
            }
            listView.SelectedItem = focused.DataContext;
            return true;
        }

        private static T? FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            while (current != null)
            {
                if (current is T target)
                {
                    return target;
                }
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }

        private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T t) return t;
                var result = FindVisualChild<T>(child);
                if (result != null) return result;
            }
            return null;
        }
    }
}
