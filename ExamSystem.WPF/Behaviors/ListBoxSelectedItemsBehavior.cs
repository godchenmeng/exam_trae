using System.Collections;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace ExamSystem.WPF.Behaviors
{
    /// <summary>
    /// 让 ListBox 的 SelectedItems 可绑定的附加行为。
    /// WPF 原生不支持在 XAML 将 SelectedItems 绑定到 ViewModel，此行为提供双向同步。
    /// </summary>
    public static class ListBoxSelectedItemsBehavior
    {
        public static readonly DependencyProperty BindableSelectedItemsProperty =
            DependencyProperty.RegisterAttached(
                "BindableSelectedItems",
                typeof(IList),
                typeof(ListBoxSelectedItemsBehavior),
                new PropertyMetadata(null, OnBindableSelectedItemsChanged));

        public static IList GetBindableSelectedItems(DependencyObject obj)
            => (IList)obj.GetValue(BindableSelectedItemsProperty);

        public static void SetBindableSelectedItems(DependencyObject obj, IList value)
            => obj.SetValue(BindableSelectedItemsProperty, value);

        private static void OnBindableSelectedItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ListBox listBox)
                return;

            // 解绑旧集合事件
            if (e.OldValue is INotifyCollectionChanged oldCollection)
            {
                oldCollection.CollectionChanged -= (s, args) => OnViewModelSelectionChanged(listBox, args);
            }

            // 绑定 ListBox 选择变化事件
            listBox.SelectionChanged -= ListBox_SelectionChanged;
            listBox.SelectionChanged += ListBox_SelectionChanged;

            // 同步当前选中项到 ViewModel 集合
            var target = e.NewValue as IList;
            if (target is not null)
            {
                SyncFromListBoxToViewModel(listBox, target);

                // 监听 ViewModel 集合变化，反向同步到 ListBox
                if (target is INotifyCollectionChanged newCollection)
                {
                    newCollection.CollectionChanged += (s, args) => OnViewModelSelectionChanged(listBox, args);
                }
            }
        }

        private static void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is not ListBox listBox)
                return;

            var target = GetBindableSelectedItems(listBox);
            if (target is null)
                return;

            // 从 ListBox -> ViewModel 集合
            foreach (var item in e.RemovedItems)
            {
                if (target.Contains(item))
                    target.Remove(item);
            }
            foreach (var item in e.AddedItems)
            {
                if (!target.Contains(item))
                    target.Add(item);
            }
        }

        private static void OnViewModelSelectionChanged(ListBox listBox, NotifyCollectionChangedEventArgs e)
        {
            if (listBox.SelectionMode == SelectionMode.Single)
                return;

            // 从 ViewModel 集合 -> ListBox.SelectedItems
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                listBox.SelectedItems.Clear();
                return;
            }

            if (e.OldItems != null)
            {
                foreach (var item in e.OldItems)
                {
                    if (listBox.SelectedItems.Contains(item))
                        listBox.SelectedItems.Remove(item);
                }
            }

            if (e.NewItems != null)
            {
                foreach (var item in e.NewItems)
                {
                    if (!listBox.SelectedItems.Contains(item))
                        listBox.SelectedItems.Add(item);
                }
            }
        }

        private static void SyncFromListBoxToViewModel(ListBox listBox, IList target)
        {
            target.Clear();
            foreach (var item in listBox.SelectedItems.Cast<object>())
            {
                target.Add(item);
            }
        }
    }
}